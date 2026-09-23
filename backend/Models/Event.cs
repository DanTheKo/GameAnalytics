namespace GameAnalytics.Models;

public class Event
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public DateTime EventTimestamp { get; set; }
	public string EventData { get; set; } = "{}";

	public Guid ProjectId { get; set; }
	public Project Project { get; set; } = null!;

	public Guid? EventModelId { get; set; }
	public EventModel? EventModel { get; set; }

	public Guid? SessionId { get; set; }
	public Session? Session { get; set; }

	public Guid? UserId { get; set; }
	public User? User { get; set; }
}
