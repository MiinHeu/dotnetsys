using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class HistoryController(ApplicationDbContext db) : ControllerBase
{
	[HttpPost("log")]
	public async Task<IActionResult> Log([FromBody] AppHistoryLogDto dto)
	{
		db.AppHistoryLogs.Add(new AppHistoryLog
		{
			SessionId = dto.SessionId,
			EventType = dto.EventType,
			PoiId = dto.PoiId,
			TourId = dto.TourId,
			LanguageCode = dto.LanguageCode,
			Payload = dto.Payload,
			CreatedAt = DateTime.UtcNow
		});

		await db.SaveChangesAsync();
		return Ok();
	}

	[HttpGet]
	public async Task<IActionResult> GetHistory(
		[FromQuery] int page = 1,
		[FromQuery] int size = 50,
		[FromQuery] string? eventType = null,
		[FromQuery] string? lang = null,
		[FromQuery] DateTime? from = null,
		[FromQuery] DateTime? to = null)
	{
		var q = db.AppHistoryLogs.AsQueryable();

		if (eventType != null) q = q.Where(h => h.EventType == eventType);
		if (lang != null) q = q.Where(h => h.LanguageCode == lang);
		if (from != null) q = q.Where(h => h.CreatedAt >= from.Value);
		if (to != null) q = q.Where(h => h.CreatedAt <= to.Value);

		var total = await q.CountAsync();
		var items = await q
			.OrderByDescending(h => h.CreatedAt)
			.Skip((page - 1) * size)
			.Take(size)
			.AsNoTracking()
			.ToListAsync();

		return Ok(new { total, page, size, items });
	}
}

