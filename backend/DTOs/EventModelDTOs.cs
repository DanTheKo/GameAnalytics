using GameAnalytics.Models;

namespace GameAnalytics.DTOs;

public record CreateEventModelRequest(
	string Name,
	string Description,
	EventType EventType,
	bool IsEnabled
);

public record EventModelResponse(
	Guid Id,
	string Name,
	string Description,
	EventType EventType,
	bool IsEnabled,
	Guid ProjectId,
	List<ParameterResponse> Parameters
);
