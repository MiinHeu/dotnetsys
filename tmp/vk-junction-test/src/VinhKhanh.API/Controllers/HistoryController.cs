using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class HistoryController(ApplicationDbContext db) : ControllerBase
{
	[HttpPost("log")]
	public async Task<IActionResult> Log([FromBody] AppHistoryLogDto dto, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(dto.SessionId))
			return BadRequest(new { message = "SessionId khong duoc de trong." });

		if (string.IsNullOrWhiteSpace(dto.EventType))
			return BadRequest(new { message = "EventType khong duoc de trong." });

		var eventType = dto.EventType.Trim().ToUpperInvariant();
		if (eventType.Length > 64)
			return BadRequest(new { message = "EventType qua dai (toi da 64 ky tu)." });

		var lang = string.IsNullOrWhiteSpace(dto.LanguageCode)
			? "vi"
			: dto.LanguageCode.Trim().ToLowerInvariant();

		var payload = string.IsNullOrWhiteSpace(dto.Payload) ? null : dto.Payload.Trim();
		if (payload != null && payload.Length > 4000)
			payload = payload[..4000];

		db.AppHistoryLogs.Add(new AppHistoryLog
		{
			SessionId = dto.SessionId.Trim(),
			EventType = eventType,
			PoiId = dto.PoiId,
			TourId = dto.TourId,
			LanguageCode = lang,
			Payload = payload,
			CreatedAt = DateTime.UtcNow
		});

		await db.SaveChangesAsync(ct);
		return Ok(new { message = "Logged" });
	}

	[HttpGet]
	public async Task<IActionResult> GetHistory(
		[FromQuery] int page = 1,
		[FromQuery] int size = 50,
		[FromQuery] string? eventType = null,
		[FromQuery] string? lang = null,
		[FromQuery] DateTime? from = null,
		[FromQuery] DateTime? to = null,
		CancellationToken ct = default)
	{
		page = Math.Max(page, 1);
		size = Math.Clamp(size, 1, 500);

		var q = db.AppHistoryLogs.AsNoTracking().AsQueryable();

		if (!string.IsNullOrWhiteSpace(eventType))
		{
			var eventKey = eventType.Trim().ToUpperInvariant();
			q = q.Where(h => h.EventType == eventKey);
		}

		if (!string.IsNullOrWhiteSpace(lang))
		{
			var langKey = lang.Trim().ToLowerInvariant();
			q = q.Where(h => h.LanguageCode == langKey);
		}

		if (from is { } f)
			q = q.Where(h => h.CreatedAt >= f);
		if (to is { } t)
			q = q.Where(h => h.CreatedAt <= t);

		var total = await q.CountAsync(ct);
		var items = await q
			.OrderByDescending(h => h.CreatedAt)
			.Skip((page - 1) * size)
			.Take(size)
			.ToListAsync(ct);

		return Ok(new { total, page, size, items });
	}
}
