using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.API.Services;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AdminController(ApplicationDbContext db) : ControllerBase
{
	[Authorize(Roles = "Admin")]
	[HttpPost("seed")]
	public async Task<IActionResult> SeedDatabase(CancellationToken ct = default)
	{
		try
		{
			await db.Database.EnsureCreatedAsync(ct);
			db.SeedDemoData();
			await DbSeeder.SeedAsync(db, ct);

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
