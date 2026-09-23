using GameAnalytics.Data;
using GameAnalytics.Models;
using GameAnalytics.Services;
using Microsoft.EntityFrameworkCore;


namespace Tests
{
	public class MetricsServiceTests : IDisposable
	{
		private readonly AppDbContext _db;
		private readonly MetricsService _sut;
		private readonly Guid _projectId = Guid.NewGuid();

		public MetricsServiceTests()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			_db = new AppDbContext(options);
			_sut = new MetricsService(_db);
		}

		public void Dispose() => _db.Dispose();

		//  Helpers

		private User AddUser(DateTime firstSeen)
		{
			var user = new User
			{
				Id = Guid.NewGuid(),
				ProjectId = _projectId,
				FirstSeen = firstSeen,
			};
			_db.Users.Add(user);
			return user;
		}

		private void AddSession(Guid userId, DateTime startTime, int durationSeconds = 60)
		{
			_db.Sessions.Add(new Session
			{
				Id = Guid.NewGuid(),
				ProjectId = _projectId,
				UserId = userId,
				StartTime = startTime,
				DurationSeconds = durationSeconds,
			});
		}

		private void AddRevenueEvent(Guid userId, DateTime timestamp, double amount)
		{
			var model = _db.EventModels
				.FirstOrDefault(m => m.ProjectId == _projectId && m.EventType == EventType.Revenue)
				?? new EventModel
				{
					Id = Guid.NewGuid(),
					ProjectId = _projectId,
					EventType = EventType.Revenue,
					Name = "Purchase",
				};

			if (_db.Entry(model).State == EntityState.Detached)
				_db.EventModels.Add(model);

			_db.Events.Add(new Event
			{
				Id = Guid.NewGuid(),
				ProjectId = _projectId,
				UserId = userId,
				EventModelId = model.Id,
				EventModel = model,
				EventTimestamp = timestamp,
				EventData = $"{{\"amount\":{amount.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"currency\":\"USD\"}}",
			});
		}

		// DAU WAU MAU

		[Fact]
		public async Task GetDauWauMau_NoSessions_ReturnsEmpty()
		{
			var result = await _sut.GetDauWauMauAsync(
				_projectId,
				DateTime.UtcNow.AddDays(-7),
				DateTime.UtcNow);

			Assert.Empty(result.Dau);
			Assert.Empty(result.Wau);
			Assert.Empty(result.Mau);
		}

		[Fact]
		public async Task GetDauWauMau_SingleUserSingleDay_DauIsOne()
		{
			var user = AddUser(DateTime.UtcNow.AddDays(-3));
			var day = new DateTime(2024, 1, 10, 12, 0, 0, DateTimeKind.Utc);
			AddSession(user.Id, day);
			await _db.SaveChangesAsync();

			var result = await _sut.GetDauWauMauAsync(_projectId, day.AddDays(-1), day.AddDays(1));

			Assert.Single(result.Dau);
			Assert.Equal(1, result.Dau[0].Value);
		}

		[Fact]
		public async Task GetDauWauMau_TwoUsersSameDay_DauIsTwo()
		{
			var u1 = AddUser(DateTime.UtcNow.AddDays(-10));
			var u2 = AddUser(DateTime.UtcNow.AddDays(-10));
			var day = new DateTime(2024, 1, 10, 9, 0, 0, DateTimeKind.Utc);
			AddSession(u1.Id, day);
			AddSession(u2.Id, day.AddHours(2));
			await _db.SaveChangesAsync();

			var result = await _sut.GetDauWauMauAsync(_projectId, day.AddDays(-1), day.AddDays(1));

			Assert.Equal(2, result.Dau[0].Value);
		}

		[Fact]
		public async Task GetDauWauMau_SameUserMultipleSessionsSameDay_DauIsOne()
		{
			var user = AddUser(DateTime.UtcNow.AddDays(-5));
			var day = new DateTime(2024, 1, 15, 8, 0, 0, DateTimeKind.Utc);
			AddSession(user.Id, day);
			AddSession(user.Id, day.AddHours(3));
			AddSession(user.Id, day.AddHours(6));
			await _db.SaveChangesAsync();

			var result = await _sut.GetDauWauMauAsync(_projectId, day.AddDays(-1), day.AddDays(1));

			Assert.Equal(1, result.Dau[0].Value);
		}

		[Fact]
		public async Task GetDauWauMau_WauRolling7Days_AggregatesCorrectly()
		{
			// User active on day 0 and day 6 — both should appear in WAU for day 6
			var user = AddUser(DateTime.UtcNow.AddDays(-30));
			var base_ = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
			AddSession(user.Id, base_);
			AddSession(user.Id, base_.AddDays(6));
			await _db.SaveChangesAsync();

			var result = await _sut.GetDauWauMauAsync(_projectId, base_, base_.AddDays(6));

			var wauDay6 = result.Wau.Last();
			Assert.Equal(1, wauDay6.Value); // same user, counted once in rolling 7-day window
		}

		[Fact]
		public async Task GetDauWauMau_SessionsOutsideRange_NotCounted()
		{
			var user = AddUser(DateTime.UtcNow.AddDays(-20));
			var from = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
			var to = new DateTime(2024, 3, 7, 23, 59, 59, DateTimeKind.Utc);
			AddSession(user.Id, from.AddDays(-1)); // before range
			AddSession(user.Id, to.AddDays(1));    // after range
			await _db.SaveChangesAsync();

			var result = await _sut.GetDauWauMauAsync(_projectId, from, to);

			Assert.Empty(result.Dau);
		}

		//  Revenue

		[Fact]
		public async Task GetRevenue_NoEvents_ReturnsZeros()
		{
			var result = await _sut.GetRevenueAsync(
				_projectId,
				DateTime.UtcNow.AddDays(-7),
				DateTime.UtcNow);

			Assert.Equal(0, result.TotalRevenue);
			Assert.Equal(0, result.Arpu);
			Assert.Equal(0, result.Arppu);
			Assert.Equal(0, result.PayingUsers);
		}

		[Fact]
		public async Task GetRevenue_SinglePurchase_TotalRevenueCorrect()
		{
			var user = AddUser(DateTime.UtcNow.AddDays(-5));
			var day = new DateTime(2024, 4, 1, 10, 0, 0, DateTimeKind.Utc);
			AddRevenueEvent(user.Id, day, 9.99);
			AddSession(user.Id, day);
			await _db.SaveChangesAsync();

			var result = await _sut.GetRevenueAsync(_projectId, day.AddDays(-1), day.AddDays(1));

			Assert.Equal(9.99, result.TotalRevenue, precision: 2);
			Assert.Equal(1, result.PayingUsers);
		}

		[Fact]
		public async Task GetRevenue_MultipleUsers_ArpuCalculatedOverAllActiveUsers()
		{
			var buyer = AddUser(DateTime.UtcNow.AddDays(-5));
			var nonBuyer = AddUser(DateTime.UtcNow.AddDays(-5));
			var day = new DateTime(2024, 4, 5, 12, 0, 0, DateTimeKind.Utc);

			AddRevenueEvent(buyer.Id, day, 10.00);
			AddSession(buyer.Id, day);
			AddSession(nonBuyer.Id, day);
			await _db.SaveChangesAsync();

			var result = await _sut.GetRevenueAsync(_projectId, day.AddDays(-1), day.AddDays(1));

			// 2 active users, $10 revenue → ARPU = 5
			Assert.Equal(5.0, result.Arpu, precision: 2);
			// 1 paying user, $10 revenue → ARPPU = 10
			Assert.Equal(10.0, result.Arppu, precision: 2);
			// 1 of 2 active users paid → 50 %
			Assert.Equal(50.0, result.PayerConversionRate, precision: 2);
		}

		[Fact]
		public async Task GetRevenue_DailyAggregation_GroupsByDate()
		{
			var user = AddUser(DateTime.UtcNow.AddDays(-10));
			var day1 = new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc);
			var day2 = new DateTime(2024, 5, 2, 10, 0, 0, DateTimeKind.Utc);
			AddRevenueEvent(user.Id, day1, 5.00);
			AddRevenueEvent(user.Id, day1, 3.00);
			AddRevenueEvent(user.Id, day2, 7.00);
			AddSession(user.Id, day1);
			await _db.SaveChangesAsync();

			var result = await _sut.GetRevenueAsync(_projectId, day1, day2.AddDays(1));

			Assert.Equal(2, result.DailyRevenue.Count);
			Assert.Equal(8.00, result.DailyRevenue[0].Value, precision: 2);
			Assert.Equal(7.00, result.DailyRevenue[1].Value, precision: 2);
		}

		[Fact]
		public async Task GetRevenue_MalformedEventData_TreatedAsZero()
		{
			var user = AddUser(DateTime.UtcNow.AddDays(-3));
			var day = new DateTime(2024, 5, 10, 8, 0, 0, DateTimeKind.Utc);
			var model = new EventModel
			{
				Id = Guid.NewGuid(),
				ProjectId = _projectId,
				EventType = EventType.Revenue,
				Name = "Purchase",
			};
			_db.EventModels.Add(model);
			_db.Events.Add(new Event
			{
				Id = Guid.NewGuid(),
				ProjectId = _projectId,
				UserId = user.Id,
				EventModelId = model.Id,
				EventModel = model,
				EventTimestamp = day,
				EventData = "not-json",
			});
			await _db.SaveChangesAsync();

			var result = await _sut.GetRevenueAsync(_projectId, day.AddDays(-1), day.AddDays(1));

			Assert.Equal(0, result.TotalRevenue);
		}
		//  Retention

		[Fact]
		public async Task GetRetention_NoCohortUsers_ReturnsZeroRates()
		{
			var result = await _sut.GetRetentionAsync(
				_projectId,
				DateTime.UtcNow.AddDays(-30),
				DateTime.UtcNow);

			Assert.All(result.Classic, p => Assert.Equal(0, p.Rate));
			Assert.All(result.Rolling, p => Assert.Equal(0, p.Rate));
		}

		[Fact]
		public async Task GetRetention_ClassicDay1_UserReturnedOnExactDay()
		{
			var install = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
			var user = AddUser(install);
			AddSession(user.Id, install);               // install day
			AddSession(user.Id, install.AddDays(1));    // day 1
			await _db.SaveChangesAsync();

			var result = await _sut.GetRetentionAsync(_projectId, install, install.AddDays(1));

			var day1 = result.Classic.First(p => p.Day == 1);
			Assert.Equal(100.0, day1.Rate);
		}

		[Fact]
		public async Task GetRetention_ClassicDay1_UserDidNotReturn_ZeroRate()
		{
			var install = new DateTime(2024, 6, 5, 0, 0, 0, DateTimeKind.Utc);
			var user = AddUser(install);
			AddSession(user.Id, install);
			await _db.SaveChangesAsync();

			var result = await _sut.GetRetentionAsync(_projectId, install, install.AddDays(1));

			var day1 = result.Classic.First(p => p.Day == 1);
			Assert.Equal(0, day1.Rate);
		}

		[Fact]
		public async Task GetRetention_RollingDay7_UserReturnedOnDay10_IsRetained()
		{
			var install = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
			var user = AddUser(install);
			AddSession(user.Id, install);
			AddSession(user.Id, install.AddDays(10)); // returned after day 7
			await _db.SaveChangesAsync();

			var result = await _sut.GetRetentionAsync(_projectId, install, install.AddDays(30));

			var rolling7 = result.Rolling.First(p => p.Day == 7);
			Assert.Equal(100.0, rolling7.Rate);
		}

		[Fact]
		public async Task GetRetention_TwoUsersCohort_PartialRetention()
		{
			var install = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
			var u1 = AddUser(install);
			var u2 = AddUser(install);
			AddSession(u1.Id, install);
			AddSession(u2.Id, install);
			AddSession(u1.Id, install.AddDays(1)); // only u1 returns on day 1
			await _db.SaveChangesAsync();

			var result = await _sut.GetRetentionAsync(_projectId, install, install.AddDays(7));

			var day1 = result.Classic.First(p => p.Day == 1);
			Assert.Equal(50.0, day1.Rate);
		}

		//  Session Metrics

		[Fact]
		public async Task GetSessionMetrics_NoSessions_ReturnsEmptyAndZero()
		{
			var result = await _sut.GetSessionMetricsAsync(
				_projectId,
				DateTime.UtcNow.AddDays(-7),
				DateTime.UtcNow);

			Assert.Empty(result.AvgSessionDuration);
			Assert.Empty(result.SessionsPerUser);
			Assert.Equal(0, result.OverallAvgDurationSeconds);
		}

		[Fact]
		public async Task GetSessionMetrics_AvgDurationByDay_CorrectAverage()
		{
			var user = AddUser(DateTime.UtcNow.AddDays(-5));
			var day = new DateTime(2024, 9, 1, 10, 0, 0, DateTimeKind.Utc);
			AddSession(user.Id, day, 60);
			AddSession(user.Id, day.AddHours(2), 120);
			await _db.SaveChangesAsync();

			var result = await _sut.GetSessionMetricsAsync(_projectId, day, day.AddDays(1));

			Assert.Single(result.AvgSessionDuration);
			Assert.Equal(90.0, result.AvgSessionDuration[0].Value);
		}

		[Fact]
		public async Task GetSessionMetrics_SessionsPerUser_MultipleSessions()
		{
			var u1 = AddUser(DateTime.UtcNow.AddDays(-5));
			var u2 = AddUser(DateTime.UtcNow.AddDays(-5));
			var day = new DateTime(2024, 9, 5, 0, 0, 0, DateTimeKind.Utc);
			AddSession(u1.Id, day);
			AddSession(u1.Id, day.AddHours(2));
			AddSession(u2.Id, day.AddHours(4));
			await _db.SaveChangesAsync();

			// u1 → 2 sessions, u2 → 1 session → avg = 1.5
			var result = await _sut.GetSessionMetricsAsync(_projectId, day, day.AddDays(1));

			Assert.Single(result.SessionsPerUser);
			Assert.Equal(1.5, result.SessionsPerUser[0].Value);
		}

		[Fact]
		public async Task GetSessionMetrics_OverallAvg_AcrossAllDays()
		{
			var user = AddUser(DateTime.UtcNow.AddDays(-10));
			var day1 = new DateTime(2024, 10, 1, 10, 0, 0, DateTimeKind.Utc);
			var day2 = new DateTime(2024, 10, 2, 10, 0, 0, DateTimeKind.Utc);
			AddSession(user.Id, day1, 100);
			AddSession(user.Id, day2, 200);
			await _db.SaveChangesAsync();

			var result = await _sut.GetSessionMetricsAsync(_projectId, day1, day2.AddDays(1));

			Assert.Equal(150.0, result.OverallAvgDurationSeconds);
		}

		[Fact]
		public async Task GetSessionMetrics_OnlyCountsSessionsInRange()
		{
			var user = AddUser(DateTime.UtcNow.AddDays(-10));
			var from = new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc);
			var to = new DateTime(2024, 11, 7, 23, 59, 59, DateTimeKind.Utc);
			AddSession(user.Id, from.AddDays(-1), 999); // out of range
			AddSession(user.Id, from.AddDays(2), 100); // in range
			await _db.SaveChangesAsync();

			var result = await _sut.GetSessionMetricsAsync(_projectId, from, to);

			Assert.Equal(100.0, result.OverallAvgDurationSeconds);
		}
	}

}