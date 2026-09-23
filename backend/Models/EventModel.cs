using System.ComponentModel.DataAnnotations;

namespace GameAnalytics.Models;

public class EventModel
{
	public Guid Id { get; set; } = Guid.NewGuid();
	[Required, MaxLength(100)]
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";
	public EventType EventType { get; set; }
	public bool IsEnabled { get; set; } = true;

	public Guid ProjectId { get; set; }
	public Project Project { get; set; } = null!;

	public ICollection<Parameter> Parameters { get; set; } = [];
	public ICollection<Event> Events { get; set; } = [];
}
