using System.ComponentModel.DataAnnotations;

namespace GameAnalytics.Models;

public class User
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public DateTime FirstSeen { get; set; }
	public DateTime LastSeen { get; set; }
	public int TotalSessions { get; set; }
	public double TotalRevenue { get; set; }
	[MaxLength(100)]
	public string Country { get; set; } = "";
	public Platform Platform { get; set; }
	public string DeviceInfo { get; set; } = "";

	public Guid ProjectId { get; set; }
	public Project Project { get; set; } = null!;

	public ICollection<Session> Sessions { get; set; } = [];
	public ICollection<Event> Events { get; set; } = [];
}
