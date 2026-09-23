using GameAnalytics.Data;
using GameAnalytics.DTOs;
using GameAnalytics.Models;
using GameAnalytics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameAnalytics.Controllers;


// Dashboards

[ApiController]
[Route("api/projects/{projectId:guid}/dashboards")]
public class DashboardsController(AppDbContext db) : AuthorizedController
{
	[HttpGet]
	public async Task<IActionResult> List(Guid projectId) =>
		Ok(await db.Dashboards
			.Where(d => d.ProjectId == projectId)
			.Include(d => d.Reports)
			.Select(d => ToDto(d))
			.ToListAsync());


	[HttpGet("{dashboardId:guid}")]
	public async Task<IActionResult> Get(Guid projectId, Guid dashboardId)
	{
		var d = await db.Dashboards
			.Include(d => d.Reports)
			.FirstOrDefaultAsync(d => d.Id == dashboardId && d.ProjectId == projectId);
		return d == null ? NotFound() : Ok(ToDto(d));
	}

	[HttpPost]
	public async Task<IActionResult> Create(Guid projectId, [FromBody] DashboardRequest req)
	{
		var d = new Dashboard { ProjectId = projectId, Name = req.Name, Description = req.Description };
		db.Dashboards.Add(d);
		await db.SaveChangesAsync();
		return CreatedAtAction(nameof(Get), new { projectId, dashboardId = d.Id }, ToDto(d));
	}

	[HttpPut("{dashboardId:guid}")]
	public async Task<IActionResult> Update(
		Guid projectId, Guid dashboardId, [FromBody] DashboardRequest req)
	{
		var d = await db.Dashboards.FirstOrDefaultAsync(
			d => d.Id == dashboardId && d.ProjectId == projectId);
		if (d == null) return NotFound();
		d.Name = req.Name; d.Description = req.Description;
		await db.SaveChangesAsync();
		return Ok(ToDto(d));
	}

	[HttpDelete("{dashboardId:guid}")]
	public async Task<IActionResult> Delete(Guid projectId, Guid dashboardId)
	{
		var d = await db.Dashboards.FirstOrDefaultAsync(
			d => d.Id == dashboardId && d.ProjectId == projectId);
		if (d == null) return NotFound();
		db.Dashboards.Remove(d);
		await db.SaveChangesAsync();
		return NoContent();
	}

	private static DashboardResponse ToDto(Dashboard d) => new(
		d.Id, d.Name, d.Description, d.ProjectId,
		d.Reports?.Select(r => new ReportSummary(r.Id, r.Name, r.ChartType)).ToList() ?? []);
}

// Reports  (CRUD + execute)

[ApiController]
[Route("api/projects/{projectId:guid}/reports")]
public class ReportsController(AppDbContext db, ReportService reportSvc) : AuthorizedController
{
	[HttpGet]
	public async Task<IActionResult> List(
		Guid projectId, [FromQuery] Guid? dashboardId)
	{
		var q = db.Reports.Where(r => r.ProjectId == projectId);
		if (dashboardId.HasValue)
			q = q.Where(r => r.DashboardId == dashboardId);
		return Ok(await q.Select(r => ToDto(r)).ToListAsync());
	}

	[HttpGet("{reportId:guid}")]
	public async Task<IActionResult> Get(Guid projectId, Guid reportId)
	{
		var r = await db.Reports.FirstOrDefaultAsync(
			r => r.Id == reportId && r.ProjectId == projectId);
		return r == null ? NotFound() : Ok(ToDto(r));
	}

	[HttpPost]
	public async Task<IActionResult> Create(
		Guid projectId, [FromBody] CreateReportRequest req)
	{
		var r = new Report
		{
			ProjectId = projectId,
			Name = req.Name,
			Description = req.Description,
			SqlRequest = req.SqlRequest,
			BuilderJson = req.BuilderJson,
			ChartType = req.ChartType,
			DashboardId = req.DashboardId,
		};
		db.Reports.Add(r);
		await db.SaveChangesAsync();
		return CreatedAtAction(nameof(Get), new { projectId, reportId = r.Id }, ToDto(r));
	}

	[HttpPut("{reportId:guid}")]
	public async Task<IActionResult> Update(
		Guid projectId, Guid reportId, [FromBody] UpdateReportRequest req)
	{
		var r = await db.Reports.FirstOrDefaultAsync(
			r => r.Id == reportId && r.ProjectId == projectId);
		if (r == null) return NotFound();

		r.Name = req.Name; r.Description = req.Description;
		r.SqlRequest = req.SqlRequest; r.BuilderJson = req.BuilderJson;
		r.ChartType = req.ChartType; r.DashboardId = req.DashboardId;
		await db.SaveChangesAsync();
		return Ok(ToDto(r));
	}

	[HttpDelete("{reportId:guid}")]
	public async Task<IActionResult> Delete(Guid projectId, Guid reportId)
	{
		var r = await db.Reports.FirstOrDefaultAsync(
			r => r.Id == reportId && r.ProjectId == projectId);
		if (r == null) return NotFound();
		db.Reports.Remove(r);
		await db.SaveChangesAsync();
		return NoContent();
	}

	// POST /api/projects/{id}/reports/{reportId}/execute
	// Runs the saved SQL or BuilderJson of a stored report
	[HttpPost("{reportId:guid}/execute")]
	public async Task<IActionResult> Execute(
		Guid projectId, Guid reportId,
		[FromBody] ExecuteReportRequest? overrides,
		CancellationToken ct)
	{
		var r = await db.Reports.FirstOrDefaultAsync(
			r => r.Id == reportId && r.ProjectId == projectId, ct);
		if (r == null) return NotFound();

		try
		{
			ReportResultResponse result;

			// Prefer SQL override > BuilderJson override > saved SQL > saved BuilderJson
			if (!string.IsNullOrWhiteSpace(overrides?.SqlOverride))
				result = await reportSvc.ExecuteSqlAsync(projectId, overrides.SqlOverride, ct);
			else if (!string.IsNullOrWhiteSpace(overrides?.BuilderJson))
				result = await reportSvc.ExecuteBuilderAsync(projectId, overrides.BuilderJson, ct);
			else if (!string.IsNullOrWhiteSpace(r.SqlRequest))
				result = await reportSvc.ExecuteSqlAsync(projectId, r.SqlRequest, ct);
			else if (!string.IsNullOrWhiteSpace(r.BuilderJson))
				result = await reportSvc.ExecuteBuilderAsync(projectId, r.BuilderJson, ct);
			else
				return BadRequest("Report has no SQL or builder definition.");

			return Ok(result);
		}
		catch (InvalidOperationException ex) { return BadRequest(new ErrorResponse(ex.Message)); }
		catch (Exception ex) { return StatusCode(500, new ErrorResponse("Query failed.", ex.Message)); }
	}

	// POST /api/projects/{id}/reports/preview  (ad-hoc, not saved)
	[HttpPost("preview")]
	public async Task<IActionResult> Preview(
		Guid projectId,
		[FromBody] ExecuteReportRequest req,
		CancellationToken ct)
	{
		try
		{
			ReportResultResponse result;
			if (!string.IsNullOrWhiteSpace(req.SqlOverride))
				result = await reportSvc.ExecuteSqlAsync(projectId, req.SqlOverride, ct);
			else if (!string.IsNullOrWhiteSpace(req.BuilderJson))
				result = await reportSvc.ExecuteBuilderAsync(projectId, req.BuilderJson, ct);
			else
				return BadRequest("Provide either SqlOverride or BuilderJson.");

			return Ok(result);
		}
		catch (InvalidOperationException ex) { return BadRequest(new ErrorResponse(ex.Message)); }
		catch (Exception ex) { return StatusCode(500, new ErrorResponse("Query failed.", ex.Message)); }
	}

	// GET /api/projects/{id}/reports/builder/sql-preview?builderJson=...
	// Returns the generated SQL without executing it
	[HttpGet("builder/sql-preview")]
	public IActionResult SqlPreview(Guid projectId, [FromQuery] string builderJson)
	{
		try
		{
			var sql = reportSvc.BuilderJsonToSql(projectId, builderJson);
			return Ok(new { sql });
		}
		catch (Exception ex) { return BadRequest(new ErrorResponse(ex.Message)); }
	}

	private static ReportResponse ToDto(Report r) => new(
		r.Id, r.Name, r.Description, r.SqlRequest,
		r.BuilderJson, r.ChartType, r.ProjectId, r.DashboardId);
}
