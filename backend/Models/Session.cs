namespace GameAnalytics.Models;

public class Session
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public float DurationSeconds { get; set; }
	public int EventsCount { get; set; }

	public Guid ProjectId { get; set; }
	public Project Project { get; set; } = null!;

	public Guid UserId { get; set; }
	public User User { get; set; } = null!;

	public ICollection<Event> Events { get; set; } = [];
}
