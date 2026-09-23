namespace GameAnalytics.DTOs;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, Guid UserId, DateTime ExpiresAt);
public record RegisterRequest(string Username, string Password);
