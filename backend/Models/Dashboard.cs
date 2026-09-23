using System.ComponentModel.DataAnnotations;

namespace GameAnalytics.Models;

public class Dashboard
{
	public Guid Id { get; set; } = Guid.NewGuid();
	[Required, MaxLength(200)]
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";

	public Guid ProjectId { get; set; }
	public Project Project { get; set; } = null!;

	public ICollection<Report> Reports { get; set; } = [];
}
