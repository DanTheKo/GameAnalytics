using GameAnalytics.Models;

namespace GameAnalytics.DTOs;

public record DashboardRequest(string Name, string Description);

public record DashboardResponse(
	Guid Id,
	string Name,
	string Description,
	Guid ProjectId,
	List<ReportSummary> Reports
);

public record ReportSummary(Guid Id, string Name, ReportChartType ChartType);
