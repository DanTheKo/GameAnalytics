using GameAnalytics.Data;
using GameAnalytics.DTOs;
using GameAnalytics.Models;
using GameAnalytics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameAnalytics.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, JwtService jwt) : ControllerBase
{
	// GET /api/auth/status
	// Returns whether any users exist — used for first-run setup detection
	[HttpGet("status")]
	public async Task<IActionResult> Status() =>
		Ok(new { needsSetup = !await db.SystemUsers.AnyAsync() });

	// POST /api/auth/setup
	// Only works when no users exist — creates the first admin account
	[HttpPost("setup")]
	public async Task<IActionResult> Setup([FromBody] RegisterRequest req)
	{
		if (await db.SystemUsers.AnyAsync())
			return Conflict(new ErrorResponse("Setup already completed."));

		return await CreateUser(req);
	}

	// POST /api/auth/register
	// Requires existing auth — only logged-in admins can create new users
	[Authorize]
	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterRequest req)
	{
		if (await db.SystemUsers.AnyAsync(u => u.Username == req.Username))
			return Conflict(new ErrorResponse("Username already taken."));

		return await CreateUser(req);
	}

	// POST /api/auth/login
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest req)
	{
		var user = await db.SystemUsers
			.FirstOrDefaultAsync(u => u.Username == req.Username);

		if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
			return Unauthorized(new ErrorResponse("Invalid credentials."));

		var token = jwt.Generate(user);
		return Ok(new LoginResponse(token, user.Username, user.Id, jwt.ExpiresAt()));
	}

	// GET /api/auth/me  — validate token and return current user info
	[Authorize]
	[HttpGet("me")]
	public IActionResult Me()
	{
		var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
					?? User.FindFirst("sub")?.Value;
		var username = User.Identity?.Name
					?? User.FindFirst("unique_name")?.Value;
		return Ok(new { userId, username });
	}

	private async Task<IActionResult> CreateUser(RegisterRequest req)
	{
		if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length < 3)
			return BadRequest(new ErrorResponse("Username must be at least 3 characters."));
		if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
			return BadRequest(new ErrorResponse("Password must be at least 8 characters."));

		var user = new SystemUser
		{
			Username = req.Username.Trim(),
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
		};
		db.SystemUsers.Add(user);
		await db.SaveChangesAsync();

		var token = jwt.Generate(user);
		return Ok(new LoginResponse(token, user.Username, user.Id, jwt.ExpiresAt()));
	}
}

// GET /health
[ApiController]
[Route("health")]
public class HealthController(AppDbContext db) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> Health()
	{
		try
		{
			await db.Database.CanConnectAsync();
			return Ok(new { status = "ok", database = "connected", time = DateTime.UtcNow });
		}
		catch (Exception ex)
		{
			return StatusCode(503, new { status = "degraded", database = "error", error = ex.Message });
		}
	}
}
