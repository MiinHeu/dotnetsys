using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class MovementController(ApplicationDbContext db) : ControllerBase
{
	[AllowAnonymous, HttpPost("batch")]
	public async Task<IActionResult> BatchLog([FromBody] MovementBatchDto dto)
	{
		if (dto.Points.Count == 0) return Ok();

		db.MovementLogs.AddRange(dto.Points.Select(p => new MovementLog
		{
			SessionId = dto.SessionId,
			Latitude = p.Lat,
			Longitude = p.Lon,
			AccuracyMeters = p.Accuracy,
			RecordedAt = p.Timestamp
		}));

		await db.SaveChangesAsync();
		return Ok(new { saved = dto.Points.Count });
	}

	[Authorize(Roles = "Admin"), HttpGet("heatmap")]
	public async Task<IActionResult> GetHeatmap([FromQuery] int hours = 24)
	{
		var since = DateTime.UtcNow.AddHours(-hours);

		return Ok(await db.MovementLogs
			.Where(m => m.RecordedAt >= since)
			.Select(m => new { m.Latitude, m.Longitude })
			.ToListAsync());
	}

	[Authorize(Roles = "Admin"), HttpGet("session/{sessionId}")]
	public async Task<IActionResult> GetSession(string sessionId)
	{
		return Ok(await db.MovementLogs
			.Where(m => m.SessionId == sessionId)
			.OrderBy(m => m.RecordedAt)
			.Select(m => new { m.Latitude, m.Longitude, m.RecordedAt })
			.ToListAsync());
	}
}

