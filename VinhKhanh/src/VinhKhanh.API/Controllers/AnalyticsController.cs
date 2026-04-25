using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AnalyticsController(ApplicationDbContext db, IMemoryCache _cache, ILogger<AnalyticsController> logger) : ControllerBase
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
		
		// Cho phép thêm MANUAL cho các lượt nghe chủ động từ người dùng
		if (trigger is not ("GPS" or "QR" or "MANUAL"))
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

		try
		{
			await db.SaveChangesAsync(ct);
			
			// XÓA CACHE ĐỂ DASHBOARD CẬP NHẬT NGAY LẬP TỨC
			_cache.Remove("admin_summary_stats");
			
			return Ok(new { message = "Logged" });
		}
		catch (OperationCanceledException)
		{
			return NoContent();
		}
	}

	[HttpGet("top")]
	public async Task<IActionResult> GetTop([FromQuery] int days = 7, CancellationToken ct = default)
	{
		days = Math.Clamp(days, 1, 365);
		var since = DateTime.UtcNow.AddDays(-days);

		try
		{
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
				.AsNoTracking()
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
		catch (OperationCanceledException)
		{
			return NoContent();
		}
	}

	[HttpGet("heatmap")]
	public async Task<IActionResult> GetHeatmap([FromQuery] double hours = 24, CancellationToken ct = default)
	{
		hours = Math.Clamp(hours, 0.01, 24 * 30); // Min ~36 seconds
		string cacheKey = $"analytics_heatmap_{hours}";

		try
		{
			var points = await _cache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30); // Lower cache for live data
				var since = DateTime.UtcNow.AddHours(-hours);

				// Group by SessionId and rounded coordinates (5 decimal places ~ 1.1m)
				// to represent actual people density instead of activity duration.
				return await db.MovementLogs
					.AsNoTracking()
					.Where(m => m.RecordedAt >= since)
					.GroupBy(m => new
					{
						m.SessionId,
						Lat = Math.Round(m.Latitude, 5),
						Lon = Math.Round(m.Longitude, 5)
					})
					.Select(g => new { Latitude = g.Key.Lat, Longitude = g.Key.Lon })
					.ToListAsync(ct);
			});

			return Ok(points);
		}
		catch (OperationCanceledException)
		{
			return NoContent();
		}
	}

	[HttpGet("poi-heatmap-stats")]
	public async Task<IActionResult> GetPoiHeatmapStats([FromQuery] double hours = 24, CancellationToken ct = default)
	{
		hours = Math.Clamp(hours, 0.01, 24 * 30);
		var since = DateTime.UtcNow.AddHours(-hours);

		try
		{
			// 1. Get all active POIs
			var pois = await db.Pois
				.AsNoTracking()
				.Where(p => p.IsActive)
				.Select(p => new { p.Id, p.Name, p.Latitude, p.Longitude, p.TriggerRadiusMeters })
				.ToListAsync(ct);

			// 2. Get unique visitor positions in the timeframe
			// We group by SessionId + rounded Lat/Lon to get a clean set of "visit points"
			var visitorPoints = await db.MovementLogs
				.AsNoTracking()
				.Where(m => m.RecordedAt >= since)
				.GroupBy(m => new
				{
					m.SessionId,
					Lat = Math.Round(m.Latitude, 5),
					Lon = Math.Round(m.Longitude, 5)
				})
				.Select(g => new { g.Key.SessionId, Latitude = g.Key.Lat, Longitude = g.Key.Lon })
				.ToListAsync(ct);

			// 3. Match visitors to POIs (Simple proximity check)
			// Factor for meters to lat/lon degrees (Approx for HCM City)
			const double latFactor = 111000.0;
			const double lonFactor = 109000.0;

			var stats = pois.Select(poi =>
			{
				// Count unique SessionIds that came within the POI's radius
				var visitorCount = visitorPoints
					.Where(p =>
					{
						var dLat = (p.Latitude - poi.Latitude) * latFactor;
						var dLon = (p.Longitude - poi.Longitude) * lonFactor;
						var dist = Math.Sqrt(dLat * dLat + dLon * dLon);
						return dist <= poi.TriggerRadiusMeters;
					})
					.Select(p => p.SessionId)
					.Distinct()
					.Count();

				return new
				{
					poi.Id,
					poi.Name,
					visitorCount
				};
			})
			.OrderByDescending(x => x.visitorCount)
			.ToList();

			return Ok(stats);
		}
		catch (OperationCanceledException)
		{
			return NoContent();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error calculating POI heatmap stats");
			return StatusCode(500, "Internal server error");
		}
	}
}
