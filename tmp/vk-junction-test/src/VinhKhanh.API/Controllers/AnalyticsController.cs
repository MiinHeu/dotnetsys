using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AnalyticsController(ApplicationDbContext db) : ControllerBase
{
	[HttpPost("log")]
	public async Task<IActionResult> LogVisit([FromBody] VisitLogDto dto, CancellationToken ct = default)
	{
		if (dto.PoiId <= 0)
			return BadRequest(new { message = "PoiId khong hop le." });

		if (string.IsNullOrWhiteSpace(dto.SessionId))
			return BadRequest(new { message = "SessionId khong duoc de trong." });

		var poiExists = await db.Pois.AnyAsync(p => p.Id == dto.PoiId, ct);
		if (!poiExists)
			return BadRequest(new { message = "POI khong ton tai hoac da vo hieu." });

		var lang = string.IsNullOrWhiteSpace(dto.LanguageCode) ? "vi" : dto.LanguageCode.Trim().ToLowerInvariant();
		var trigger = string.IsNullOrWhiteSpace(dto.TriggerType) ? "GPS" : dto.TriggerType.Trim().ToUpperInvariant();
		if (trigger is not ("GPS" or "QR"))
			trigger = "GPS";

		db.PoiVisitLogs.Add(new PoiVisitLog
		{
			PoiId = dto.PoiId,
			SessionId = dto.SessionId.Trim(),
			LanguageCode = lang,
			TriggerType = trigger,
			ListenDurationSeconds = Math.Clamp(dto.Duration, 0, 7200),
			VisitedAt = DateTime.UtcNow
		});

		await db.SaveChangesAsync(ct);
		return Ok(new { message = "Logged" });
	}

	[HttpGet("top")]
	public async Task<IActionResult> GetTop([FromQuery] int days = 7, CancellationToken ct = default)
	{
		days = Math.Clamp(days, 1, 365);
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
			.ThenBy(x => x.PoiId)
			.Take(20)
			.ToListAsync(ct);

		if (top.Count == 0)
			return Ok(Array.Empty<object>());

		var poiIds = top.Select(x => x.PoiId).Distinct().ToList();
		var names = await db.Pois.IgnoreQueryFilters()
			.Where(p => poiIds.Contains(p.Id))
			.Select(p => new { p.Id, p.Name })
			.ToDictionaryAsync(x => x.Id, x => x.Name, ct);

		var result = top.Select(x => new
		{
			x.PoiId,
			PoiName = names.TryGetValue(x.PoiId, out var n) ? n : null,
			x.Count,
			x.AvgDuration
		});

		return Ok(result);
	}

	[HttpGet("heatmap")]
	public async Task<IActionResult> GetHeatmap([FromQuery] int hours = 24, CancellationToken ct = default)
	{
		hours = Math.Clamp(hours, 1, 24 * 30);
		var since = DateTime.UtcNow.AddHours(-hours);

		var points = await db.MovementLogs
			.Where(m => m.RecordedAt >= since)
			.Select(m => new { m.Latitude, m.Longitude })
			.ToListAsync(ct);

		return Ok(points);
	}
}
