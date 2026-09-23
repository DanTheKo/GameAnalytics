using GameAnalytics.Models;

namespace GameAnalytics.DTOs;

public record CreateReportRequest(
	string Name,
	string Description,
	string SqlRequest,
	string? BuilderJson,
	ReportChartType ChartType,
	Guid? DashboardId
);

public record UpdateReportRequest(
	string Name,
	string Description,
	string SqlRequest,
	string? BuilderJson,
	ReportChartType ChartType,
	Guid? DashboardId
);

public record ReportResponse(
	Guid Id,
	string Name,
	string Description,
	string SqlRequest,
	string? BuilderJson,
	ReportChartType ChartType,
	Guid ProjectId,
	Guid? DashboardId
);

public record ExecuteReportRequest(
	string? SqlOverride,
	string? BuilderJson
);

public record ReportResultResponse(
	List<string> Columns,
	List<Dictionary<string, object?>> Rows,
	int TotalRows,
	long ExecutionMs
);
