using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.API.Services;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(ApplicationDbContext db) : ControllerBase
{
	[HttpGet("summary")]
	public async Task<IActionResult> Summary(CancellationToken ct = default)
	{
		var totalPois = await db.Pois.AsNoTracking().CountAsync(ct);
		var totalTours = await db.Tours.AsNoTracking().CountAsync(ct);
		var totalUsers = await db.AppUsers.AsNoTracking().CountAsync(ct);
		var totalVisits = await db.PoiVisitLogs.AsNoTracking().CountAsync(ct);

		var ownerStats = await db.Pois.AsNoTracking()
			.Where(p => p.OwnerUserId.HasValue)
			.GroupBy(p => p.OwnerUserId!.Value)
			.Select(g => new { OwnerId = g.Key, PoiCount = g.Count() })
			.ToListAsync(ct);

		return Ok(new { totalPois, totalTours, totalUsers, totalVisits, ownerStats });
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
}
