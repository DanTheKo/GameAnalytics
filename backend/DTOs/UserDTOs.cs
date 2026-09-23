using GameAnalytics.Models;

namespace GameAnalytics.DTOs;

public record UpsertUserRequest(
	Guid UserId,
	string Country,
	string Platform,
	string DeviceInfo
);

public record UserResponse(
	Guid Id,
	DateTime FirstSeen,
	DateTime LastSeen,
	int TotalSessions,
	double TotalRevenue,
	string Country,
	Platform Platform,
	string DeviceInfo
);
