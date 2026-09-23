namespace GameAnalytics.DTOs;

public record StartSessionRequest(Guid UserId, DateTime StartTime);
public record EndSessionRequest(DateTime EndTime);

public record SessionResponse(
	Guid Id,
	DateTime StartTime,
	DateTime EndTime,
	float DurationSeconds,
	int EventsCount,
	Guid UserId
);
