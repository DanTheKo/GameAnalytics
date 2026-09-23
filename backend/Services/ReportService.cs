using GameAnalytics.Data;
using GameAnalytics.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;

namespace GameAnalytics.Services;

public class ReportService(AppDbContext db)
{
	//  Execute raw SQL (scoped to project — safety wrapper)

	public async Task<ReportResultResponse> ExecuteSqlAsync(
		Guid projectId, string sql, CancellationToken ct = default)
	{
		var upper = sql.Trim().ToUpperInvariant();
		var blocked = new[] { "INSERT", "UPDATE", "DELETE", "DROP", "TRUNCATE", "ALTER", "CREATE", "EXEC" };
		if (blocked.Any(b => upper.StartsWith(b) || upper.Contains($" {b} ")))
			throw new InvalidOperationException("Only SELECT statements are allowed in reports.");

		if (!upper.Contains("PROJECTID"))
		{

			sql = $"""
                SELECT * FROM ({sql}) AS _report_query
                """;
		}

		var sw = Stopwatch.StartNew();

		await using var conn = db.Database.GetDbConnection();
		await conn.OpenAsync(ct);
		await using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;
		cmd.CommandTimeout = 30;

		var pidParam = cmd.CreateParameter();
		pidParam.ParameterName = "projectId";
		pidParam.Value = projectId;
		cmd.Parameters.Add(pidParam);

		await using var reader = await cmd.ExecuteReaderAsync(ct);

		var columns = Enumerable.Range(0, reader.FieldCount)
			.Select(i => reader.GetName(i))
			.ToList();

		var rows = new List<Dictionary<string, object?>>();
		while (await reader.ReadAsync(ct))
		{
			var row = new Dictionary<string, object?>();
			for (int i = 0; i < reader.FieldCount; i++)
				row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
			rows.Add(row);
		}

		sw.Stop();
		return new ReportResultResponse(columns, rows, rows.Count, sw.ElapsedMilliseconds);
	}



	public string BuilderJsonToSql(Guid projectId, string builderJson)
	{
		using var doc = System.Text.Json.JsonDocument.Parse(builderJson);
		var root = doc.RootElement;

		string metric = GetStr(root, "metric", "events");
		string aggregate = GetStr(root, "aggregate", "count");
		string groupBy = GetStr(root, "groupBy", "day");
		string dateField = GetStr(root, "dateField", "EventTimestamp");
		string valField = GetStr(root, "valueField", "amount");

		string table = metric switch
		{
			"sessions" => "\"Sessions\"",
			"users" => "\"Users\"",
			_ => "\"Events\""
		};

		// Date range filter from builder
		string? fromDate = root.TryGetProperty("from", out var fd) ? fd.GetString() : null;
		string? toDate = root.TryGetProperty("to", out var td) ? td.GetString() : null;

		// Determine the date field per table
		dateField = metric switch
		{
			"sessions" => "StartTime",
			"users" => "FirstSeen",
			_ => "EventTimestamp"
		};

		// Parameter field to group by (extracted from EventData JSON)
		string? groupByParameter = root.TryGetProperty("groupByParameter", out var gbp)
			&& gbp.ValueKind == System.Text.Json.JsonValueKind.String
			? gbp.GetString() : null;

		string paramExpr = groupByParameter != null
			? $"(\"EventData\"::jsonb->>'{SanitizeIdentifier(groupByParameter)}')"
			: "NULL";

		// Label expression — human-readable string for date groupings
		string labelExpr = groupBy switch
		{
			"week" => $"TO_CHAR(DATE_TRUNC('week', \"{dateField}\"), 'Mon DD')",
			"month" => $"TO_CHAR(DATE_TRUNC('month', \"{dateField}\"), 'Mon YYYY')",
			"country" => "\"Country\"",
			"platform" => "\"Platform\"::text",
			"event_name" => "em.\"Name\"",
			"parameter" => paramExpr,
			_ => $"TO_CHAR(DATE_TRUNC('day', \"{dateField}\"), 'Mon DD')"
		};

		// ORDER BY expression — sort on the actual truncated date, not the label string
		string orderExpr = groupBy switch
		{
			"week" => $"DATE_TRUNC('week', \"{dateField}\")",
			"month" => $"DATE_TRUNC('month', \"{dateField}\")",
			"country" => "\"Country\"",
			"platform" => "\"Platform\"::text",
			"event_name" => "em.\"Name\"",
			"parameter" => $"(CASE WHEN {paramExpr} ~ '^-?\\d+(\\.\\d+)?$' THEN {paramExpr}::numeric END), {paramExpr}",
			_ => $"DATE_TRUNC('day', \"{dateField}\")"
		};

		// Aggregate expression
		string aggExpr = aggregate switch
		{
			"sum" => $"SUM((\"EventData\"::jsonb->>'{valField}')::float)",
			"avg" => $"AVG((\"EventData\"::jsonb->>'{valField}')::float)",
			"distinct_users" => "COUNT(DISTINCT \"UserId\")",
			_ => "COUNT(*)"
		};

		string? eventModelId = root.TryGetProperty("eventModelId", out var emi)
			&& emi.ValueKind == System.Text.Json.JsonValueKind.String
			? emi.GetString() : null;


		// Filters
		var filters = new StringBuilder();
		filters.Append($"\"ProjectId\" = '{projectId}'");
		if (!eventModelId.IsNullOrEmpty())
			filters.Append($" AND t.\"EventModelId\" = '{Sanitize(eventModelId)}'");
		if (fromDate != null && !String.IsNullOrWhiteSpace(fromDate))
			filters.Append($" AND t.\"{dateField}\" >= '{Sanitize(fromDate)}'::timestamptz");
		if (toDate != null && !String.IsNullOrWhiteSpace(toDate))
			filters.Append($" AND t.\"{dateField}\" <= '{Sanitize(toDate)}'::timestamptz");

		if (root.TryGetProperty("filters", out var filtersEl))
		{
			foreach (var f in filtersEl.EnumerateArray())
			{
				string field = f.GetProperty("field").GetString()!;
				string op = f.GetProperty("op").GetString()!;
				var val = f.GetProperty("value");

				string safeField = $"\"{SanitizeIdentifier(field)}\"";

				string condition = op switch
				{
					"eq" => $"{safeField} = '{Sanitize(val.GetString()!)}'",
					"neq" => $"{safeField} <> '{Sanitize(val.GetString()!)}'",
					"gt" => $"{safeField} > {val.GetDouble()}",
					"lt" => $"{safeField} < {val.GetDouble()}",
					"in" => $"{safeField} IN ({string.Join(",", val.EnumerateArray().Select(v => $"'{Sanitize(v.GetString()!)}'"))})",
					_ => "1=1"
				};
				filters.Append($" AND {condition}");
			}
		}

		// JOIN EventModel if needed
		string join = (metric == "events" && (groupBy == "event_name" || eventModelId == null))
			? "LEFT JOIN \"EventModels\" em ON t.\"EventModelId\" = em.\"Id\""
			: "";

		string sql = $"""
            SELECT
                {labelExpr} AS "label",
                {aggExpr}   AS "value"
            FROM {table} t
            {join}
            WHERE {filters}
            GROUP BY {labelExpr}, {orderExpr}
            ORDER BY {orderExpr}
            """;

		return sql;
	}

	public async Task<ReportResultResponse> ExecuteBuilderAsync(
		Guid projectId, string builderJson, CancellationToken ct = default)
	{
		string sql = BuilderJsonToSql(projectId, builderJson);
		return await ExecuteSqlAsync(projectId, sql, ct);
	}

	// Helpers

	private static string GetStr(System.Text.Json.JsonElement el, string key, string def) =>
		el.TryGetProperty(key, out var v) ? v.GetString() ?? def : def;

	private static string Sanitize(string s) =>
		s.Replace("'", "''").Replace(";", "");

	private static string SanitizeIdentifier(string s) =>
		new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
}