namespace GameAnalytics.DTOs;

public record IngestEventRequest(
	string EventName,
	DateTime EventTimestamp,
	string EventData,
	Guid? SessionId,
	Guid? UserId
);

public record BatchIngestRequest(List<IngestEventRequest> Events);

public record IngestResponse(Guid EventId, bool Accepted, string? Error);

public record EventBrowseResponse(
	Guid Id,
	DateTime EventTimestamp,
	string EventData,
	string? EventModelName,
	Guid? EventModelId,
	Guid? SessionId,
	Guid? UserId
);
