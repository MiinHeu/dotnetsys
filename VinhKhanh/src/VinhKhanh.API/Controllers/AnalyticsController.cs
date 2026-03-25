using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AnalyticsController(ApplicationDbContext db) : ControllerBase
{
	[HttpPost("log")]
	public async Task<IActionResult> LogVisit([FromBody] VisitLogDto dto)
	{
		db.PoiVisitLogs.Add(new PoiVisitLog
		{
			PoiId = dto.PoiId,
			SessionId = dto.SessionId,
			LanguageCode = dto.LanguageCode,
			TriggerType = dto.TriggerType,
			ListenDurationSeconds = dto.Duration,
			VisitedAt = DateTime.UtcNow
		});

		await db.SaveChangesAsync();
		return Ok();
	}

	[HttpGet("top")]
	public async Task<IActionResult> GetTop([FromQuery] int days = 7)
	{
		var since = DateTime.UtcNow.AddDays(-days);

		var top = await db.PoiVisitLogs
			.Where(v => v.VisitedAt >= since)
			.GroupBy(v => v.PoiId)
			.Select(g => new
			{
				PoiId = g.Key,
				Count = g.Count(),
				AvgDuration = g.Average(v => v.ListenDurationSeconds)
			})
			.OrderByDescending(x => x.Count)
			.Take(10)
			.ToListAsync();

		return Ok(top);
	}

	[HttpGet("heatmap")]
	public async Task<IActionResult> GetHeatmap([FromQuery] int hours = 24)
	{
		var since = DateTime.UtcNow.AddHours(-hours);

		return Ok(await db.MovementLogs
			.Where(m => m.RecordedAt >= since)
			.Select(m => new { m.Latitude, m.Longitude })
			.ToListAsync());
	}
}

