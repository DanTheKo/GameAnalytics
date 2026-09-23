using System.ComponentModel.DataAnnotations;

namespace GameAnalytics.Models;

public class SystemUser
{
	public Guid Id { get; set; } = Guid.NewGuid();
	[Required, MaxLength(100)]
	public string Username { get; set; } = "";
	[Required]
	public string PasswordHash { get; set; } = "";

	public ICollection<Project> Projects { get; set; } = [];
}
