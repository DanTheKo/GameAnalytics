using GameAnalytics.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GameAnalytics.Services;

public class JwtService(IConfiguration config)
{
	private readonly JwtSettings _settings = config
		.GetSection("JwtSettings")
		.Get<JwtSettings>()!;

	public string Generate(SystemUser user)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		};

		var token = new JwtSecurityToken(
			issuer: _settings.Issuer,
			audience: _settings.Audience,
			claims: claims,
			expires: DateTime.UtcNow.AddHours(_settings.ExpiryHours),
			signingCredentials: creds
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	public DateTime ExpiresAt() =>
		DateTime.UtcNow.AddHours(_settings.ExpiryHours);
}

public class JwtSettings
{
	public string SecretKey { get; set; } = "";
	public string Issuer { get; set; } = "GameAnalytics";
	public string Audience { get; set; } = "GameAnalyticsDashboard";
	public int ExpiryHours { get; set; } = 8;
}
