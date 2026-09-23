namespace GameAnalytics.DTOs;

public record DateRangeRequest(DateTime From, DateTime To);
public record PagedResponse<T>(List<T> Items, int Total, int Page, int PageSize);
public record ErrorResponse(string Message, string? Detail = null);
