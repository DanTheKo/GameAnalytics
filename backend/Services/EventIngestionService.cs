using GameAnalytics.Data;
using GameAnalytics.DTOs;
using GameAnalytics.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GameAnalytics.Services;

public class EventIngestionService(AppDbContext db)
{
	public async Task<List<IngestResponse>> IngestBatchAsync(
		Guid projectId, BatchIngestRequest request, CancellationToken ct = default)
	{
		// Load all event models for this project once
		var eventModels = await db.EventModels
			.Where(em => em.ProjectId == projectId && em.IsEnabled)
			.ToDictionaryAsync(em => em.Name.ToLower(), ct);

		var results = new List<IngestResponse>();

		foreach (var req in request.Events)
		{
			try
			{
				var result = await IngestSingleAsync(projectId, req, eventModels, ct);
				results.Add(result);
			}
			catch (Exception ex)
			{
				results.Add(new IngestResponse(Guid.Empty, false, ex.Message));
			}
		}

		await db.SaveChangesAsync(ct);
		return results;
	}

	private async Task<IngestResponse> IngestSingleAsync(
		Guid projectId,
		IngestEventRequest req,
		Dictionary<string, EventModel> eventModels,
		CancellationToken ct)
	{
		// Resolve event model (optional — unknown events are still stored)
		eventModels.TryGetValue(req.EventName.ToLower(), out var model);

		/*if (model != null && !TryValidateEventData(model, req.EventData, out var validationError))
        {
            return new IngestResponse(Guid.Empty, false, validationError!);
        }*/

		// Treat empty string as null — SDK sends "" when no session is active
		Guid? sessionId = req.SessionId.HasValue && req.SessionId != Guid.Empty
			? req.SessionId : null;

		Guid? userId = req.UserId.HasValue && req.UserId != Guid.Empty
			? req.UserId : null;

		// Validate session belongs to this project before linking
		if (sessionId.HasValue)
		{
			var sessionExists = await db.Sessions
				.AnyAsync(s => s.Id == sessionId && s.ProjectId == projectId, ct);
			if (!sessionExists)
			{
				sessionId = null;
				// Don't fail the event — just drop the session link
			}
		}

		// Upsert user if provided
		if (req.UserId.HasValue)
		{
			var user = await db.Users.FindAsync([req.UserId.Value], ct);
			if (user != null)
			{
				user.LastSeen = req.EventTimestamp > user.LastSeen
					? req.EventTimestamp : user.LastSeen;
			}

		}

		var ev = new Event
		{
			ProjectId = projectId,
			EventModelId = model?.Id,
			SessionId = req.SessionId,
			UserId = req.UserId,
			EventTimestamp = req.EventTimestamp,
			EventData = req.EventData,
		};

		db.Events.Add(ev);

		// Update session event count if linked
		if (req.SessionId.HasValue)
		{
			var session = await db.Sessions.FindAsync([req.SessionId.Value], ct);
			if (session != null) session.EventsCount++;
		}

		return new IngestResponse(ev.Id, true, null);
	}


	public async Task<Session> StartSessionAsync(
		Guid projectId, StartSessionRequest req, CancellationToken ct = default)
	{
		var session = new Session
		{
			ProjectId = projectId,
			UserId = req.UserId,
			StartTime = req.StartTime,
			EndTime = req.StartTime,   // will be updated on end
		};

		db.Sessions.Add(session);

		// Update user's session count
		var user = await db.Users.FindAsync([req.UserId], ct);
		if (user != null) user.TotalSessions++;

		await db.SaveChangesAsync(ct);
		return session;
	}

	public async Task<Session> EndSessionAsync(
		Guid projectId, Guid sessionId, EndSessionRequest req, CancellationToken ct = default)
	{
		var session = await db.Sessions
			.FirstOrDefaultAsync(s => s.Id == sessionId && s.ProjectId == projectId, ct)
			?? throw new KeyNotFoundException("Session not found.");

		session.EndTime = req.EndTime;
		session.DurationSeconds = (float)(req.EndTime - session.StartTime).TotalSeconds;

		await db.SaveChangesAsync(ct);
		return session;
	}


	public async Task<User> UpsertUserAsync(
		Guid projectId, UpsertUserRequest req, CancellationToken ct = default)
	{
		if (!Enum.TryParse<Platform>(req.Platform, ignoreCase: true, out var platform))
			platform = Platform.iOS;

		var user = await db.Users.FindAsync([req.UserId], ct);

		if (user == null)
		{
			user = new User
			{
				Id = req.UserId,
				ProjectId = projectId,
				FirstSeen = DateTime.UtcNow,
				LastSeen = DateTime.UtcNow,
				Country = req.Country,
				Platform = platform,
				DeviceInfo = req.DeviceInfo,
			};
			db.Users.Add(user);
		}
		else
		{
			user.LastSeen = DateTime.UtcNow;
			user.Country = req.Country;
			user.Platform = platform;
			user.DeviceInfo = req.DeviceInfo;
		}

		await db.SaveChangesAsync(ct);
		return user;
	}

	private static bool TryValidateEventData(
		EventModel model, string eventDataJson, out string? error)
	{
		JsonDocument doc;
		try
		{
			doc = JsonDocument.Parse(eventDataJson);
		}
		catch (JsonException ex)
		{
			error = $"Invalid JSON in EventData: {ex.Message}";
			return false;
		}

		using (doc)
		{
			var root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				error = "EventData must be a JSON object";
				return false;
			}

			foreach (var param in model.Parameters)
			{
				if (!root.TryGetProperty(param.Name, out var value))
				{
					error = $"Missing parameter '{param.Name}'";
					return false;
				}

				if (!MatchesDataType(value, param.DataType))
				{
					error = $"Parameter '{param.Name}' expected {param.DataType}, got {value.ValueKind}";
					return false;
				}
			}
		}

		error = null;
		return true;
	}

	private static bool MatchesDataType(JsonElement value, DataType dataType) => dataType switch
	{
		DataType.String => value.ValueKind == JsonValueKind.String,
		DataType.Int => value.ValueKind == JsonValueKind.Number,
		DataType.Float => value.ValueKind == JsonValueKind.Number,
		DataType.Bool => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
		DataType.DateTime => value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out _),
		_ => true
	};
}
