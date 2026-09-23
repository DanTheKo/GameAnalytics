using GameAnalytics.Models;
using Microsoft.EntityFrameworkCore;

namespace GameAnalytics.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<SystemUser> SystemUsers => Set<SystemUser>();
	public DbSet<Project> Projects => Set<Project>();
	public DbSet<Dashboard> Dashboards => Set<Dashboard>();
	public DbSet<Report> Reports => Set<Report>();
	public DbSet<EventModel> EventModels => Set<EventModel>();
	public DbSet<Parameter> Parameters => Set<Parameter>();
	public DbSet<Session> Sessions => Set<Session>();
	public DbSet<User> Users => Set<User>();
	public DbSet<Event> Events => Set<Event>();

	protected override void OnModelCreating(ModelBuilder b)
	{
		// SystemUser
		b.Entity<SystemUser>(e =>
		{
			e.HasIndex(u => u.Username).IsUnique();
		});

		// Project
		b.Entity<Project>(e =>
		{
			e.HasIndex(p => p.ApiKey).IsUnique();
			e.HasOne(p => p.SystemUser)
			 .WithMany(u => u.Projects)
			 .HasForeignKey(p => p.SystemUserId)
			 .OnDelete(DeleteBehavior.Cascade);
		});

		// Dashboard
		b.Entity<Dashboard>(e =>
		{
			e.HasOne(d => d.Project)
			 .WithMany(p => p.Dashboards)
			 .HasForeignKey(d => d.ProjectId)
			 .OnDelete(DeleteBehavior.Cascade);
		});

		// Report
		b.Entity<Report>(e =>
		{
			e.HasOne(r => r.Project)
			 .WithMany(p => p.Reports)
			 .HasForeignKey(r => r.ProjectId)
			 .OnDelete(DeleteBehavior.Cascade);

			e.HasOne(r => r.Dashboard)
			 .WithMany(d => d.Reports)
			 .HasForeignKey(r => r.DashboardId)
			 .OnDelete(DeleteBehavior.SetNull);
		});

		// EventModel
		b.Entity<EventModel>(e =>
		{
			e.HasOne(em => em.Project)
			 .WithMany(p => p.EventModels)
			 .HasForeignKey(em => em.ProjectId)
			 .OnDelete(DeleteBehavior.Cascade);
		});

		// Parameter
		b.Entity<Parameter>(e =>
		{
			e.HasOne(p => p.Project)
			 .WithMany(pr => pr.Parameters)
			 .HasForeignKey(p => p.ProjectId)
			 .OnDelete(DeleteBehavior.Cascade);

			e.HasOne(p => p.EventModel)
			 .WithMany(em => em.Parameters)
			 .HasForeignKey(p => p.EventModelId)
			 .OnDelete(DeleteBehavior.SetNull);
		});

		// Session 
		b.Entity<Session>(e =>
		{
			e.HasOne(s => s.Project)
			 .WithMany(p => p.Sessions)
			 .HasForeignKey(s => s.ProjectId)
			 .OnDelete(DeleteBehavior.Cascade);

			e.HasOne(s => s.User)
			 .WithMany(u => u.Sessions)
			 .HasForeignKey(s => s.UserId)
			 .OnDelete(DeleteBehavior.Restrict);

			e.HasIndex(s => new { s.ProjectId, s.StartTime });
		});

		// User
		b.Entity<User>(e =>
		{
			e.HasOne(u => u.Project)
			 .WithMany(p => p.Users)
			 .HasForeignKey(u => u.ProjectId)
			 .OnDelete(DeleteBehavior.Cascade);

			e.HasIndex(u => new { u.ProjectId, u.FirstSeen });
			e.HasIndex(u => new { u.ProjectId, u.LastSeen });
		});

		// Event 
		b.Entity<Event>(e =>
		{
			e.HasOne(ev => ev.Project)
			 .WithMany(p => p.Events)
			 .HasForeignKey(ev => ev.ProjectId)
			 .OnDelete(DeleteBehavior.Cascade);

			e.HasOne(ev => ev.EventModel)
			 .WithMany(em => em.Events)
			 .HasForeignKey(ev => ev.EventModelId)
			 .OnDelete(DeleteBehavior.SetNull);

			e.HasOne(ev => ev.Session)
			 .WithMany(s => s.Events)
			 .HasForeignKey(ev => ev.SessionId)
			 .OnDelete(DeleteBehavior.SetNull);

			e.HasOne(ev => ev.User)
			 .WithMany(u => u.Events)
			 .HasForeignKey(ev => ev.UserId)
			 .OnDelete(DeleteBehavior.SetNull);

			// Most queries filter by project + time
			e.HasIndex(ev => new { ev.ProjectId, ev.EventTimestamp });
			e.HasIndex(ev => new { ev.ProjectId, ev.EventModelId, ev.EventTimestamp });
		});
	}
}
