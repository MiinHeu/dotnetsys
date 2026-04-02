using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class MovementController(ApplicationDbContext db) : ControllerBase
{
	[HttpPost("batch")]
	public async Task<IActionResult> BatchLog([FromBody] MovementBatchDto dto, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(dto.SessionId))
			return BadRequest(new { message = "SessionId khong duoc de trong." });

		if (dto.Points == null || dto.Points.Count == 0)
			return Ok(new { saved = 0, dropped = 0 });

		if (dto.Points.Count > 2000)
			return BadRequest(new { message = "So diem gui len qua lon (toi da 2000 diem/batch)." });

		var now = DateTime.UtcNow;
		var validLogs = new List<MovementLog>(dto.Points.Count);

		foreach (var p in dto.Points)
		{
			if (!IsValidCoordinate(p.Lat, p.Lon))
				continue;

			var timestamp = p.Timestamp == default
				? now
				: (p.Timestamp.Kind == DateTimeKind.Unspecified
					? DateTime.SpecifyKind(p.Timestamp, DateTimeKind.Utc)
					: p.Timestamp.ToUniversalTime());

			validLogs.Add(new MovementLog
			{
				SessionId = dto.SessionId.Trim(),
				Latitude = p.Lat,
				Longitude = p.Lon,
				AccuracyMeters = p.Accuracy < 0 ? 0 : p.Accuracy,
				RecordedAt = timestamp
			});
		}

		if (validLogs.Count == 0)
			return BadRequest(new { message = "Khong co diem GPS hop le trong batch." });

		db.MovementLogs.AddRange(validLogs);
		await db.SaveChangesAsync(ct);

		return Ok(new { saved = validLogs.Count, dropped = dto.Points.Count - validLogs.Count });
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

	[HttpGet("session/{sessionId}")]
	public async Task<IActionResult> GetSession(string sessionId, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(sessionId))
			return BadRequest(new { message = "SessionId khong hop le." });

		var points = await db.MovementLogs
			.Where(m => m.SessionId == sessionId)
			.OrderBy(m => m.RecordedAt)
			.Select(m => new { m.Latitude, m.Longitude, m.RecordedAt })
			.ToListAsync(ct);

		return Ok(points);
	}

	private static bool IsValidCoordinate(double lat, double lon)
		=> lat is >= -90 and <= 90 && lon is >= -180 and <= 180;
}
