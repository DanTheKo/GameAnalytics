using System.ComponentModel.DataAnnotations;

namespace GameAnalytics.Models;

public class Report
{
	public Guid Id { get; set; } = Guid.NewGuid();
	[Required, MaxLength(200)]
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";
	public string SqlRequest { get; set; } = "";
	public string? BuilderJson { get; set; }
	public ReportChartType ChartType { get; set; } = ReportChartType.Line;

	public Guid ProjectId { get; set; }
	public Project Project { get; set; } = null!;

	public Guid? DashboardId { get; set; }
	public Dashboard? Dashboard { get; set; }
}
