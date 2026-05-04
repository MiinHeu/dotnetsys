using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VinhKhanh.API.Services;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(ApplicationDbContext db, IConnectionTracker tracker, IMemoryCache _cache) : ControllerBase
{
	[HttpGet("summary")]
	public async Task<IActionResult> Summary(CancellationToken ct = default)
	{
		const string CacheKey = "admin_summary_stats";

		var result = await _cache.GetOrCreateAsync(CacheKey, async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

			var totalPois = await db.Pois.AsNoTracking().CountAsync(ct);
			var totalTours = await db.Tours.AsNoTracking().CountAsync(ct);
			
			// AppUsers are administrative accounts (Admin, Owner).
			// To count unique app users (installs), we count distinct SessionIds from logs.
			var totalHistoryDevices = await db.AppHistoryLogs.AsNoTracking().Select(x => x.SessionId).Distinct().CountAsync(ct);
			var totalVisitDevices = await db.PoiVisitLogs.AsNoTracking().Select(x => x.SessionId).Distinct().CountAsync(ct);
			var totalDevices = (new[] { totalHistoryDevices, totalVisitDevices }).Max();

			var totalVisits = await db.PoiVisitLogs.AsNoTracking().CountAsync(ct);

			var ownerStats = await db.Pois.AsNoTracking()
				.Where(p => p.OwnerUserId.HasValue)
				.GroupBy(p => p.OwnerUserId!.Value)
				.Select(g => new { OwnerId = g.Key, PoiCount = g.Count() })
				.ToListAsync(ct);

			return new { totalPois, totalTours, totalDevices, totalVisits, ownerStats };
		});

		// activeUsers is real-time, get it fresh every time
		var activeUsers = tracker.GetOnlineCount();

		return Ok(new
		{
			result.totalPois,
			result.totalTours,
			result.totalDevices,
			result.totalVisits,
			activeUsers,
			result.ownerStats
		});
	}

	[Authorize(Roles = "Admin")]
	[HttpPost("seed")]
	public async Task<IActionResult> SeedDatabase(CancellationToken ct = default)
	{
		try
		{
			await db.Database.MigrateAsync(ct);
			await DbSeeder.SeedAsync(db, forceDefaultCredentials: false, ct);

			var summary = new
			{
				users = await db.AppUsers.CountAsync(ct),
				pois = await db.Pois.IgnoreQueryFilters().CountAsync(ct),
				tours = await db.Tours.IgnoreQueryFilters().CountAsync(ct)
			};

			return Ok(new { message = "Database seeded successfully", summary });
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = $"Seeding failed: {ex.Message}" });
		}
	}

	/// <summary>
	/// Danh sách phiên du khách — phân trang, lọc theo ngày.
	/// Admin xem tất cả. Owner chỉ xem phiên liên quan đến quán của mình.
	/// </summary>
	[Authorize(Roles = "Admin,Owner")]
	[HttpGet("sessions")]
	public async Task<IActionResult> GetSessions(
		[FromQuery] int page = 1,
		[FromQuery] int size = 50,
		[FromQuery] int days = 7,
		[FromQuery] string? platform = null,
		CancellationToken ct = default)
	{
		page = Math.Max(page, 1);
		size = Math.Clamp(size, 1, 200);
		days = Math.Clamp(days, 1, 365);
		var since = DateTime.UtcNow.AddDays(-days);

		var q = db.DeviceSessions.AsNoTracking()
			.Where(s => s.StartedAt >= since);

		// Owner chỉ xem phiên có liên quan đến quán của mình
		var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
		if (role == "Owner")
		{
			var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
			var ownerPoiIds = await db.Pois.IgnoreQueryFilters()
				.Where(p => p.OwnerUserId == userId)
				.Select(p => p.Id)
				.ToListAsync(ct);

			var relatedSessionIds = await db.PoiVisitLogs.IgnoreQueryFilters()
				.Where(v => ownerPoiIds.Contains(v.PoiId) && v.VisitedAt >= since)
				.Select(v => v.SessionId)
				.Distinct()
				.ToListAsync(ct);

			q = q.Where(s => relatedSessionIds.Contains(s.SessionId));
		}

		if (!string.IsNullOrWhiteSpace(platform))
			q = q.Where(s => s.DevicePlatform == platform.Trim());

		var total = await q.CountAsync(ct);
		var items = await q
			.OrderByDescending(s => s.StartedAt)
			.Skip((page - 1) * size)
			.Take(size)
			.Select(s => new
			{
				s.Id,
				s.SessionId,
				s.DeviceModel,
				s.DevicePlatform,
				s.OsVersion,
				s.AppVersion,
				s.Manufacturer,
				s.StartedAt,
				s.EndedAt,
				DurationMinutes = s.EndedAt != null
					? Math.Round((s.EndedAt.Value - s.StartedAt).TotalMinutes, 1)
					: Math.Round((DateTime.UtcNow - s.StartedAt).TotalMinutes, 1),
				s.PoisVisited,
				DistanceMeters = Math.Round(s.DistanceMeters),
				s.LanguageUsed,
				s.IsReturning,
				IsActive = s.EndedAt == null && (DateTime.UtcNow - s.LastHeartbeatAt).TotalMinutes < 3
			})
			.ToListAsync(ct);

		return Ok(new { total, page, size, items });
	}

	/// <summary>
	/// Thống kê tổng hợp phiên: thiết bị phổ biến, thời gian TB, giờ cao điểm, tỷ lệ quay lại.
	/// </summary>
	[Authorize(Roles = "Admin")]
	[HttpGet("sessions/stats")]
	public async Task<IActionResult> GetSessionStats(
		[FromQuery] int days = 30,
		CancellationToken ct = default)
	{
		days = Math.Clamp(days, 1, 365);
		var since = DateTime.UtcNow.AddDays(-days);

		var sessions = await db.DeviceSessions.AsNoTracking()
			.Where(s => s.StartedAt >= since)
			.ToListAsync(ct);

		if (sessions.Count == 0)
			return Ok(new
			{
				totalSessions = 0,
				avgDurationMinutes = 0.0,
				avgPoisVisited = 0.0,
				avgDistanceMeters = 0.0,
				returningRate = 0.0,
				platformBreakdown = Array.Empty<object>(),
				topDevices = Array.Empty<object>(),
				topManufacturers = Array.Empty<object>(),
				languageBreakdown = Array.Empty<object>()
			});

		// Thời gian TB (chỉ tính phiên đã kết thúc)
		var ended = sessions.Where(s => s.EndedAt != null).ToList();
		var avgDuration = ended.Count > 0
			? Math.Round(ended.Average(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes), 1)
			: 0;

		// Tỷ lệ quay lại
		var returningCount = sessions.Count(s => s.IsReturning);
		var returningRate = Math.Round(100.0 * returningCount / sessions.Count, 1);

		// Phân bổ Android / iOS
		var platformBreakdown = sessions
			.GroupBy(s => s.DevicePlatform)
			.Select(g => new { platform = g.Key, count = g.Count() })
			.OrderByDescending(x => x.count)
			.ToList();

		// Top 5 thiết bị
		var topDevices = sessions
			.GroupBy(s => s.DeviceModel)
			.Select(g => new { model = g.Key, count = g.Count() })
			.OrderByDescending(x => x.count)
			.Take(5)
			.ToList();

		// Top nhà sản xuất
		var topManufacturers = sessions
			.GroupBy(s => s.Manufacturer)
			.Select(g => new { manufacturer = g.Key, count = g.Count() })
			.OrderByDescending(x => x.count)
			.Take(5)
			.ToList();

		// Phân bổ ngôn ngữ
		var languageBreakdown = sessions
			.GroupBy(s => s.LanguageUsed)
			.Select(g => new { language = g.Key, count = g.Count() })
			.OrderByDescending(x => x.count)
			.ToList();

		return Ok(new
		{
			totalSessions = sessions.Count,
			avgDurationMinutes = avgDuration,
			avgPoisVisited = Math.Round(sessions.Average(s => s.PoisVisited), 1),
			avgDistanceMeters = Math.Round(sessions.Average(s => s.DistanceMeters)),
			returningRate,
			platformBreakdown,
			topDevices,
			topManufacturers,
			languageBreakdown
		});
	}

	/// <summary>
	/// Giờ cao điểm: phân tích số phiên theo từng khung giờ trong ngày.
	/// </summary>
	[Authorize(Roles = "Admin")]
	[HttpGet("peak-hours")]
	public async Task<IActionResult> GetPeakHours(
		[FromQuery] int days = 7,
		CancellationToken ct = default)
	{
		days = Math.Clamp(days, 1, 365);
		var since = DateTime.UtcNow.AddDays(-days);

		var sessions = await db.DeviceSessions.AsNoTracking()
			.Where(s => s.StartedAt >= since)
			.Select(s => s.StartedAt)
			.ToListAsync(ct);

		// Chuyển sang giờ Việt Nam (UTC+7) để phân tích chính xác
		var hourCounts = Enumerable.Range(0, 24).Select(h => new
		{
			hour = h,
			count = sessions.Count(s => s.AddHours(7).Hour == h)
		}).ToList();

		return Ok(hourCounts);
	}
}
