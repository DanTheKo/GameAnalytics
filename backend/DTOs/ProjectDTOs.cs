namespace GameAnalytics.DTOs;

public record ProjectRequest(string Name, string Description);

public record ProjectResponse(
	Guid Id,
	string Name,
	string Description,
	string ApiKey
);
