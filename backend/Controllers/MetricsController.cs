using GameAnalytics.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameAnalytics.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/metrics")]
public class MetricsController(MetricsService metrics) : AuthorizedController
{
	// GET /api/projects/{id}/metrics/dau-wau-mau?from=...&to=...
	[HttpGet("dau-wau-mau")]
	public async Task<IActionResult> GetDauWauMau(
		Guid projectId,
		[FromQuery] DateTime from,
		[FromQuery] DateTime to)
	{
		if (from >= to) return BadRequest("'from' must be before 'to'.");
		var result = await metrics.GetDauWauMauAsync(projectId, from, to);
		return Ok(result);
	}

	// GET /api/projects/{id}/metrics/revenue?from=...&to=...
	[HttpGet("revenue")]
	public async Task<IActionResult> GetRevenue(
		Guid projectId,
		[FromQuery] DateTime from,
		[FromQuery] DateTime to)
	{
		if (from >= to) return BadRequest("'from' must be before 'to'.");
		var result = await metrics.GetRevenueAsync(projectId, from, to);
		return Ok(result);
	}

	// GET /api/projects/{id}/metrics/retention?from=...&to=...
	[HttpGet("retention")]
	public async Task<IActionResult> GetRetention(
		Guid projectId,
		[FromQuery] DateTime from,
		[FromQuery] DateTime to)
	{
		if (from >= to) return BadRequest("'from' must be before 'to'.");
		var result = await metrics.GetRetentionAsync(projectId, from, to);
		return Ok(result);
	}

	// GET /api/projects/{id}/metrics/sessions?from=...&to=...
	[HttpGet("sessions")]
	public async Task<IActionResult> GetSessionMetrics(
		Guid projectId,
		[FromQuery] DateTime from,
		[FromQuery] DateTime to)
	{
		if (from >= to) return BadRequest("'from' must be before 'to'.");
		var result = await metrics.GetSessionMetricsAsync(projectId, from, to);
		return Ok(result);
	}


	[HttpGet("user-acquisition")]
	public async Task<IActionResult> GetUserAcquisition(
	Guid projectId, [FromQuery] DateTime from, [FromQuery] DateTime to)
	{
		if (from >= to) return BadRequest("'from' must be before 'to'.");
		return Ok(await metrics.GetUserAcquisitionAsync(projectId, from, to));
	}
}
