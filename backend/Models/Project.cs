using System.ComponentModel.DataAnnotations;

namespace GameAnalytics.Models;

public class Project
{
	public Guid Id { get; set; } = Guid.NewGuid();
	[Required, MaxLength(200)]
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";
	public string ApiKey { get; set; } = Guid.NewGuid().ToString("N");

	public Guid SystemUserId { get; set; }
	public SystemUser SystemUser { get; set; } = null!;

	public ICollection<Dashboard> Dashboards { get; set; } = [];
	public ICollection<Report> Reports { get; set; } = [];
	public ICollection<Event> Events { get; set; } = [];
	public ICollection<EventModel> EventModels { get; set; } = [];
	public ICollection<Session> Sessions { get; set; } = [];
	public ICollection<User> Users { get; set; } = [];
	public ICollection<Parameter> Parameters { get; set; } = [];
}
