using GameAnalytics.Data;
using GameAnalytics.DTOs;
using GameAnalytics.Models;
using GameAnalytics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameAnalytics.Controllers;


//sdk ingest

[ApiController]
[Route("api/ingest")]
public class IngestionController(AppDbContext db, EventIngestionService svc) : ControllerBase
{
	// Resolve project from X-Api-Key header
	private async Task<Project?> ResolveProjectAsync(CancellationToken ct) =>
		Request.Headers.TryGetValue("X-Api-Key", out var key)
			? await db.Projects.FirstOrDefaultAsync(p => p.ApiKey == key.ToString(), ct)
			: null;

	// POST /api/ingest/events
	[HttpPost("events")]
	public async Task<IActionResult> IngestBatch(
		[FromBody] BatchIngestRequest request,
		CancellationToken ct)
	{
		var project = await ResolveProjectAsync(ct);
		if (project == null) return Unauthorized("Invalid or missing X-Api-Key.");

		var results = await svc.IngestBatchAsync(project.Id, request, ct);
		return Ok(results);
	}

	// POST /api/ingest/sessions/start
	[HttpPost("sessions/start")]
	public async Task<IActionResult> StartSession(
		[FromBody] StartSessionRequest request,
		CancellationToken ct)
	{
		var project = await ResolveProjectAsync(ct);
		if (project == null) return Unauthorized("Invalid or missing X-Api-Key.");

		var session = await svc.StartSessionAsync(project.Id, request, ct);
		return Ok(new SessionResponse(
			session.Id, session.StartTime, session.EndTime,
			session.DurationSeconds, session.EventsCount, session.UserId));
	}

	// POST /api/ingest/sessions/{sessionId}/end
	[HttpPost("sessions/{sessionId:guid}/end")]
	public async Task<IActionResult> EndSession(
		Guid sessionId,
		[FromBody] EndSessionRequest request,
		CancellationToken ct)
	{
		var project = await ResolveProjectAsync(ct);
		if (project == null) return Unauthorized("Invalid or missing X-Api-Key.");

		try
		{
			var session = await svc.EndSessionAsync(project.Id, sessionId, request, ct);
			return Ok(new SessionResponse(
				session.Id, session.StartTime, session.EndTime,
				session.DurationSeconds, session.EventsCount, session.UserId));
		}
		catch (KeyNotFoundException e) { return NotFound(e.Message); }
	}

	// POST /api/ingest/users
	[HttpPost("users")]
	public async Task<IActionResult> UpsertUser(
		[FromBody] UpsertUserRequest request,
		CancellationToken ct)
	{
		var project = await ResolveProjectAsync(ct);
		if (project == null) return Unauthorized("Invalid or missing X-Api-Key.");

		try
		{
			var user = await svc.UpsertUserAsync(project.Id, request, ct);
			return Ok(new UserResponse(user.Id, user.FirstSeen, user.LastSeen,
				user.TotalSessions, user.TotalRevenue, user.Country, user.Platform, user.DeviceInfo));
		}
		catch (KeyNotFoundException e) { return NotFound(e.Message); }

	}
}


//event models

[ApiController]
[Route("api/projects/{projectId:guid}/event-models")]
public class EventModelsController(AppDbContext db) : AuthorizedController
{
	// GET /api/projects/{id}/event-models
	[HttpGet]
	public async Task<IActionResult> List(Guid projectId) =>
		Ok(await db.EventModels
			.Where(em => em.ProjectId == projectId)
			.Include(em => em.Parameters)
			.Select(em => ToDto(em))
			.ToListAsync());

	// GET /api/projects/{id}/event-models/{modelId}
	[HttpGet("{modelId:guid}")]
	public async Task<IActionResult> Get(Guid projectId, Guid modelId)
	{
		var em = await db.EventModels.Include(e => e.Parameters)
			.FirstOrDefaultAsync(e => e.Id == modelId && e.ProjectId == projectId);
		return em == null ? NotFound() : Ok(ToDto(em));
	}

	// POST /api/projects/{id}/event-models
	[HttpPost]
	public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateEventModelRequest req)
	{
		var em = new EventModel
		{
			ProjectId = projectId,
			Name = req.Name,
			Description = req.Description,
			EventType = req.EventType,
			IsEnabled = req.IsEnabled,
		};
		db.EventModels.Add(em);
		await db.SaveChangesAsync();
		return CreatedAtAction(nameof(Get), new { projectId, modelId = em.Id }, ToDto(em));
	}

	// PUT /api/projects/{id}/event-models/{modelId}
	[HttpPut("{modelId:guid}")]
	public async Task<IActionResult> Update(
		Guid projectId, Guid modelId, [FromBody] CreateEventModelRequest req)
	{
		var em = await db.EventModels.FirstOrDefaultAsync(
			e => e.Id == modelId && e.ProjectId == projectId);
		if (em == null) return NotFound();

		em.Name = req.Name; em.Description = req.Description;
		em.EventType = req.EventType; em.IsEnabled = req.IsEnabled;
		await db.SaveChangesAsync();
		return Ok(ToDto(em));
	}

	// DELETE /api/projects/{id}/event-models/{modelId}
	[HttpDelete("{modelId:guid}")]
	public async Task<IActionResult> Delete(Guid projectId, Guid modelId)
	{
		var em = await db.EventModels.FirstOrDefaultAsync(
			e => e.Id == modelId && e.ProjectId == projectId);
		if (em == null) return NotFound();
		db.EventModels.Remove(em);
		await db.SaveChangesAsync();
		return NoContent();
	}

	// POST /api/projects/{id}/event-models/{modelId}/parameters
	[HttpPost("{modelId:guid}/parameters")]
	public async Task<IActionResult> AddParameter(
		Guid projectId, Guid modelId, [FromBody] CreateParameterRequest req)
	{
		var em = await db.EventModels.FirstOrDefaultAsync(
			e => e.Id == modelId && e.ProjectId == projectId);
		if (em == null) return NotFound();

		var param = new Parameter
		{
			ProjectId = projectId,
			EventModelId = modelId,
			Name = req.Name,
			ParameterType = req.ParameterType,
			DataType = req.DataType,
		};
		db.Parameters.Add(param);
		await db.SaveChangesAsync();
		return Ok(ToParamDto(param));
	}

	// DELETE /api/projects/{id}/event-models/{modelId}/parameters/{paramId}
	[HttpDelete("{modelId:guid}/parameters/{paramId:guid}")]
	public async Task<IActionResult> DeleteParameter(
		Guid projectId, Guid modelId, Guid paramId)
	{
		var p = await db.Parameters.FirstOrDefaultAsync(
			p => p.Id == paramId && p.ProjectId == projectId && p.EventModelId == modelId);
		if (p == null) return NotFound();
		db.Parameters.Remove(p);
		await db.SaveChangesAsync();
		return NoContent();
	}

	private static EventModelResponse ToDto(EventModel em) => new(
		em.Id, em.Name, em.Description, em.EventType, em.IsEnabled, em.ProjectId,
		em.Parameters?.Select(ToParamDto).ToList() ?? []);

	private static ParameterResponse ToParamDto(Parameter p) => new(
		p.Id, p.Name, p.ParameterType, p.DataType, p.ProjectId, p.EventModelId);
}

[ApiController]
[Route("api/projects/{projectId:guid}/events")]
public class EventsBrowseController(AppDbContext db) : AuthorizedController
{
	// GET /api/projects/{id}/events?eventModelId=...&from=...&to=...&page=1&pageSize=50
	[HttpGet]
	public async Task<IActionResult> Browse(
		Guid projectId,
		[FromQuery] Guid? eventModelId,
		[FromQuery] DateTime? from,
		[FromQuery] DateTime? to,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 50)
	{
		pageSize = Math.Clamp(pageSize, 1, 200);

		var q = db.Events
			.Include(e => e.EventModel)
			.Where(e => e.ProjectId == projectId);

		if (eventModelId.HasValue) q = q.Where(e => e.EventModelId == eventModelId);
		if (from.HasValue) q = q.Where(e => e.EventTimestamp >= from);
		if (to.HasValue) q = q.Where(e => e.EventTimestamp <= to);

		var total = await q.CountAsync();

		var items = await q
			.OrderByDescending(e => e.EventTimestamp)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(e => new EventBrowseResponse(
				e.Id,
				e.EventTimestamp,
				e.EventData,
				e.EventModel != null ? e.EventModel.Name : null,
				e.EventModelId,
				e.SessionId,
				e.UserId))
			.ToListAsync();

		return Ok(new PagedResponse<EventBrowseResponse>(items, total, page, pageSize));
	}
}
