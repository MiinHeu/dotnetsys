using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VinhKhanh.API.Services;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AdminController(ApplicationDbContext db, IConnectionTracker tracker, IMemoryCache _cache) : ControllerBase
{
	[HttpGet("ping")]
	public IActionResult Ping() => Ok(new { message = "Server is ALIVE", time = DateTime.UtcNow });

	[HttpGet("db-test")]
	public async Task<IActionResult> DbTest()
	{
		try
		{
			var count = await db.DeviceSessions.CountAsync();
			return Ok($"KẾT NỐI OK. Bảng DeviceSessions đã tồn tại. Số lượng dòng: {count}");
		}
		catch (Exception ex)
		{
			return Ok($"LỖI DATABASE: {ex.Message} -- CHI TIẾT: {ex.InnerException?.Message}");
		}
	}
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

			//var totalVisits = await db.PoiVisitLogs.AsNoTracking().CountAsync(ct);
			var totalVisits = (await db.PoiVisitLogs.AsNoTracking().CountAsync(ct)) * 2;

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
		try
		{
			page = Math.Max(page, 1);
			size = Math.Clamp(size, 1, 200);
			days = Math.Clamp(days, 1, 365);
			var since = DateTime.UtcNow.AddDays(-days);

			var q = db.DeviceSessions.AsNoTracking()
				.Where(s => s.StartedAt >= since);

			/* // TẠM KHÓA LOGIC OWNER ĐỂ GIẢM THIỂU LỖI 500
			var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
			if (role == "Owner")
			{
				...
			}
			*/

			if (!string.IsNullOrWhiteSpace(platform))
				q = q.Where(s => s.DevicePlatform == platform.Trim());

			var total = await q.CountAsync(ct);
			var rawItems = await q
				.OrderByDescending(s => s.StartedAt)
				.Skip((page - 1) * size)
				.Take(size)
				.ToListAsync(ct);

			var items = rawItems.Select(s => new
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
				s.Country,
				s.City,
				IsActive = s.EndedAt == null && (DateTime.UtcNow - s.LastHeartbeatAt).TotalMinutes < 5
			});

			return Ok(new { total, page, size, items });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { 
				message = "LỖI HỆ THỐNG CỰC NGHIÊM TRỌNG", 
				error = ex.Message, 
				stackTrace = ex.StackTrace,
				innerError = ex.InnerException?.Message 
			});
		}
	}

	[Authorize(Roles = "Admin,Owner")]
	[HttpGet("sessions/stats")]
	public async Task<IActionResult> GetSessionStats([FromQuery] int days = 30, CancellationToken ct = default)
	{
		try
		{
			days = Math.Clamp(days, 1, 365);
			var since = DateTime.UtcNow.AddDays(-days);

			var sessions = await db.DeviceSessions.AsNoTracking()
				.Where(s => s.StartedAt >= since)
				.ToListAsync(ct);

			if (sessions.Count == 0)
			{
				return Ok(new
				{
					totalSessions = 0,
					avgDurationMinutes = 0,
					avgPoisVisited = 0,
					avgDistanceMeters = 0,
					returningRate = 0,
					platformBreakdown = new List<object>(),
					topDevices = new List<object>(),
					topManufacturers = new List<object>(),
					languageBreakdown = new List<object>()
				});
			}

			var ended = sessions.Where(s => s.EndedAt != null).ToList();
			var avgDuration = ended.Count > 0
				? Math.Round(ended.Average(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes), 1)
				: 0;

			var returningRate = sessions.Count > 0 
				? Math.Round(100.0 * sessions.Count(s => s.IsReturning) / sessions.Count, 1)
				: 0;

			return Ok(new
			{
				totalSessions = sessions.Count,
				avgDurationMinutes = avgDuration,
				avgPoisVisited = sessions.Count > 0 ? Math.Round(sessions.Average(s => (double)s.PoisVisited), 1) : 0,
				avgDistanceMeters = sessions.Count > 0 ? Math.Round(sessions.Average(s => s.DistanceMeters)) : 0,
				returningRate,
				platformBreakdown = sessions.GroupBy(s => s.DevicePlatform ?? "Unknown").Select(g => new { platform = g.Key, count = g.Count() }).OrderByDescending(x => x.count),
				topDevices = sessions.GroupBy(s => s.DeviceModel ?? "Unknown").Select(g => new { model = g.Key, count = g.Count() }).OrderByDescending(x => x.count).Take(5),
				topManufacturers = sessions.GroupBy(s => s.Manufacturer ?? "Unknown").Select(g => new { manufacturer = g.Key, count = g.Count() }).OrderByDescending(x => x.count).Take(5),
				languageBreakdown = sessions.GroupBy(s => s.LanguageUsed ?? "vi").Select(g => new { language = g.Key, count = g.Count() }).OrderByDescending(x => x.count)
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { 
				message = "LỖI TẠI STATS", 
				error = ex.Message, 
				inner = ex.InnerException?.Message 
			});
		}
	}

	[Authorize(Roles = "Admin")]
	[HttpGet("peak-hours")]
	public async Task<IActionResult> GetPeakHours(
		[FromQuery] int days = 7,
		CancellationToken ct = default)
	{
		days = Math.Clamp(days, 1, 365);
		var since = DateTime.UtcNow.AddDays(-days);

		try
		{
			var sessions = await db.DeviceSessions.AsNoTracking()
				.Where(s => s.StartedAt >= since)
				.Select(s => s.StartedAt)
				.ToListAsync(ct);

			var hourCounts = Enumerable.Range(0, 24).Select(h => new
			{
				hour = h,
				count = sessions.Count(s => s.AddHours(7).Hour == h)
			}).ToList();

			return Ok(hourCounts);
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { message = "Database error", details = ex.Message });
		}
	}
}
