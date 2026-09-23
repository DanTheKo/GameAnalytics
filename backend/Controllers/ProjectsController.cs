using GameAnalytics.Data;
using GameAnalytics.DTOs;
using GameAnalytics.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameAnalytics.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(AppDbContext db) : AuthorizedController
{
	// GET /api/projects?userId={guid}
	[HttpGet]
	public async Task<IActionResult> List([FromQuery] Guid userId) =>
		Ok(await db.Projects
			.Where(p => p.SystemUserId == userId)
			.Select(p => ToDto(p))
			.ToListAsync());

	// GET /api/projects/{id}
	[HttpGet("{projectId:guid}")]
	public async Task<IActionResult> Get(Guid projectId)
	{
		var p = await db.Projects.FindAsync(projectId);
		return p == null ? NotFound() : Ok(ToDto(p));
	}

	// POST /api/projects
	[HttpPost]
	public async Task<IActionResult> Create(
		[FromBody] ProjectRequest req,
		[FromQuery] Guid userId)
	{
		if (!await db.SystemUsers.AnyAsync(u => u.Id == userId))
			return BadRequest(new ErrorResponse("SystemUser not found."));

		var p = new Project
		{
			SystemUserId = userId,
			Name = req.Name,
			Description = req.Description,
		};
		db.Projects.Add(p);
		await db.SaveChangesAsync();
		return CreatedAtAction(nameof(Get), new { projectId = p.Id }, ToDto(p));
	}

	// PUT /api/projects/{id}
	[HttpPut("{projectId:guid}")]
	public async Task<IActionResult> Update(Guid projectId, [FromBody] ProjectRequest req)
	{
		var p = await db.Projects.FindAsync(projectId);
		if (p == null) return NotFound();
		p.Name = req.Name; p.Description = req.Description;
		await db.SaveChangesAsync();
		return Ok(ToDto(p));
	}

	// DELETE /api/projects/{id}
	[HttpDelete("{projectId:guid}")]
	public async Task<IActionResult> Delete(Guid projectId)
	{
		var p = await db.Projects.FindAsync(projectId);
		if (p == null) return NotFound();
		db.Projects.Remove(p);
		await db.SaveChangesAsync();
		return NoContent();
	}

	// POST /api/projects/{id}/regenerate-api-key
	[HttpPost("{projectId:guid}/regenerate-api-key")]
	public async Task<IActionResult> RegenerateApiKey(Guid projectId)
	{
		var p = await db.Projects.FindAsync(projectId);
		if (p == null) return NotFound();
		p.ApiKey = Guid.NewGuid().ToString("N");
		await db.SaveChangesAsync();
		return Ok(new { p.ApiKey });
	}

	private static ProjectResponse ToDto(Project p) =>
		new(p.Id, p.Name, p.Description, p.ApiKey);
}
