using GameAnalytics.Data;
using GameAnalytics.DTOs;
using GameAnalytics.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GameAnalytics.Services;

public class MetricsService(AppDbContext db)
{
	//  DAU, WAU, MAU
	public async Task<DauWauMauResponse> GetDauWauMauAsync(
		Guid projectId, DateTime from, DateTime to)
	{
		// Pull distinct (userId, date) from sessions in range
		var sessions = await db.Sessions
			.Where(s => s.ProjectId == projectId
					 && s.StartTime >= from
					 && s.StartTime <= to)
			.Select(s => new { s.UserId, Day = s.StartTime.Date })
			.Distinct()
			.ToListAsync();

		var dau = sessions
			.GroupBy(s => s.Day)
			.OrderBy(g => g.Key)
			.Select(g => new MetricPoint(g.Key, g.Select(x => x.UserId).Distinct().Count()))
			.ToList();

		// WAU: rolling 7-day window per day
		var wau = dau.Select(dp => new MetricPoint(
			dp.Date,
			sessions
				.Where(s => s.Day >= dp.Date.AddDays(-6) && s.Day <= dp.Date)
				.Select(s => s.UserId).Distinct().Count()
		)).ToList();

		// MAU: rolling 30-day window per day
		var mau = dau.Select(dp => new MetricPoint(
			dp.Date,
			sessions
				.Where(s => s.Day >= dp.Date.AddDays(-29) && s.Day <= dp.Date)
				.Select(s => s.UserId).Distinct().Count()
		)).ToList();

		return new DauWauMauResponse(dau, wau, mau);
	}

	//  Revenue  (events with EventType.Revenue)
	//  EventData JSON expected: { "amount": 4.99, "currency": "USD" }
	double ParseAmount(string json)
	{
		try
		{
			var doc = System.Text.Json.JsonDocument.Parse(json);
			return doc.RootElement.TryGetProperty("amount", out var v)
				? v.GetDouble() : 0;
		}
		catch { return 0; }
	}

	public async Task<RevenueMetricsResponse> GetRevenueAsync(
		Guid projectId, DateTime from, DateTime to)
	{
		var revenueEvents = await db.Events
			.Include(e => e.EventModel)
			.Where(e => e.ProjectId == projectId
					 && e.EventModel != null
					 && e.EventModel.EventType == EventType.Revenue
					 && e.EventTimestamp >= from
					 && e.EventTimestamp <= to
					 && e.UserId != null)
			.Select(e => new
			{
				e.EventTimestamp,
				e.UserId,
				e.EventData
			})
			.ToListAsync();

		// Parse amount from JSON EventData

		var byDay = revenueEvents
			.GroupBy(e => e.EventTimestamp.Date)
			.OrderBy(g => g.Key)
			.Select(g => new MetricPoint(g.Key, g.Sum(e => ParseAmount(e.EventData))))
			.ToList();

		double totalRevenue = byDay.Sum(d => d.Value);

		var payingUsers = revenueEvents
			.Where(e => ParseAmount(e.EventData) > 0)
			.Select(e => e.UserId)
			.Distinct()
			.Count();

		int activeUsers = await db.Sessions
			.Where(s => s.ProjectId == projectId
					 && s.StartTime >= from
					 && s.StartTime <= to)
			.Select(s => s.UserId)
			.Distinct()
			.CountAsync();

		double arpu = activeUsers > 0 ? totalRevenue / activeUsers : 0;
		double arppu = payingUsers > 0 ? totalRevenue / payingUsers : 0;
		double conv = activeUsers > 0 ? (double)payingUsers / activeUsers * 100 : 0;

		return new RevenueMetricsResponse(byDay, totalRevenue, arpu, arppu, payingUsers, conv);
	}

	//  Retention
	public async Task<RetentionResponse> GetRetentionAsync(
		Guid projectId, DateTime from, DateTime to)
	{
		// Users acquired in range
		var cohortUsers = await db.Users
			.Where(u => u.ProjectId == projectId
					 && u.FirstSeen >= from
					 && u.FirstSeen <= to)
			.Select(u => new { u.Id, InstallDate = u.FirstSeen.Date })
			.ToListAsync();

		// All session dates for those users
		var userIds = cohortUsers.Select(u => u.Id).ToHashSet();
		var sessionDates = await db.Sessions
			.Where(s => s.ProjectId == projectId && userIds.Contains(s.UserId))
			.Select(s => new { s.UserId, Day = s.StartTime.Date })
			.Distinct()
			.ToListAsync();

		int[] retentionDays = [1, 2, 3, 4, 5, 6, 7, 14, 21, 30];

		// Classic retention: returned on exactly day N
		var classic = retentionDays.Select(day =>
		{
			int returned = cohortUsers.Count(u =>
				sessionDates.Any(s =>
					s.UserId == u.Id &&
					s.Day == u.InstallDate.AddDays(day)));
			double rate = cohortUsers.Count > 0
				? Math.Round((double)returned / cohortUsers.Count * 100, 1)
				: 0;
			return new RetentionPoint(day, rate);
		}).ToList();

		// Rolling retention: returned on day N or after
		var rolling = retentionDays.Select(day =>
		{
			int returned = cohortUsers.Count(u =>
				sessionDates.Any(s =>
					s.UserId == u.Id &&
					s.Day >= u.InstallDate.AddDays(day)));
			double rate = cohortUsers.Count > 0
				? Math.Round((double)returned / cohortUsers.Count * 100, 1)
				: 0;
			return new RetentionPoint(day, rate);
		}).ToList();

		// Cohort table: weekly cohorts
		int[] cohortDays = [1, 7, 14, 30, 60, 90];
		var cohortTable = cohortUsers
			.GroupBy(u => StartOfWeek(u.InstallDate))
			.OrderBy(g => g.Key)
			.Select(g =>
			{
				var rates = cohortDays.Select(day =>
				{
					if (g.Key.AddDays(day) > DateTime.UtcNow.Date)
						return (double?)null;

					int returned = g.Count(u =>
						sessionDates.Any(s =>
							s.UserId == u.Id &&
							s.Day >= u.InstallDate.AddDays(day) &&
							s.Day <= u.InstallDate.AddDays(day + 1)));

					return g.Count() > 0
						? (double?)Math.Round((double)returned / g.Count() * 100, 1)
						: null;
				}).ToList();

				return new CohortRow(
					g.Key.ToString("MMM dd", CultureInfo.InvariantCulture),
					g.Count(),
					rates
				);
			}).ToList();

		return new RetentionResponse(classic, rolling, cohortTable);
	}

	//  Session Metrics

	public async Task<SessionMetricsResponse> GetSessionMetricsAsync(
		Guid projectId, DateTime from, DateTime to)
	{
		var sessions = await db.Sessions
			.Where(s => s.ProjectId == projectId
					 && s.StartTime >= from
					 && s.StartTime <= to)
			.Select(s => new { s.StartTime, s.DurationSeconds, s.UserId })
			.ToListAsync();

		var avgDuration = sessions
			.GroupBy(s => s.StartTime.Date)
			.OrderBy(g => g.Key)
			.Select(g => new MetricPoint(g.Key, Math.Round(g.Average(s => s.DurationSeconds), 1)))
			.ToList();

		var sessionsPerUser = sessions
			.GroupBy(s => s.StartTime.Date)
			.OrderBy(g => g.Key)
			.Select(g =>
			{
				var perUser = g.GroupBy(s => s.UserId).Select(u => u.Count()).ToList();
				return new MetricPoint(g.Key, perUser.Count > 0 ? Math.Round(perUser.Average(), 2) : 0);
			}).ToList();

		double overall = sessions.Count > 0 ? sessions.Average(s => s.DurationSeconds) : 0;

		return new SessionMetricsResponse(avgDuration, sessionsPerUser, overall);
	}

	public async Task<UserAcquisitionResponse> GetUserAcquisitionAsync(
	Guid projectId, DateTime from, DateTime to)
	{
		var newUsers = await db.Users
			.Where(u => u.ProjectId == projectId && u.FirstSeen >= from && u.FirstSeen <= to)
			.Select(u => new { u.Id, Day = u.FirstSeen.Date, u.Country, Platform = u.Platform.ToString() })
			.ToListAsync();

		var dailyNewUsers = newUsers
			.GroupBy(u => u.Day)
			.OrderBy(g => g.Key)
			.Select(g => new MetricPoint(g.Key, g.Count()))
			.ToList();

		var byCountry = newUsers
			.Where(u => !string.IsNullOrWhiteSpace(u.Country))
			.GroupBy(u => u.Country)
			.OrderByDescending(g => g.Count())
			.Take(10)
			.Select(g => new LabelValue(g.Key, g.Count()))
			.ToList();

		var byPlatform = newUsers
			.GroupBy(u => u.Platform)
			.OrderByDescending(g => g.Count())
			.Select(g => new LabelValue(g.Key, g.Count()))
			.ToList();

		return new UserAcquisitionResponse(dailyNewUsers, newUsers.Count, byCountry, byPlatform);
	}

	//  Helpers

	private static DateTime StartOfWeek(DateTime date)
	{
		int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
		return date.AddDays(-diff).Date;
	}
}
