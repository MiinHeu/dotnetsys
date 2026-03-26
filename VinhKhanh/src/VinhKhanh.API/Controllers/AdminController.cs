using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.API.Services;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AdminController(ApplicationDbContext db) : ControllerBase
{
	[HttpPost("seed")]
	public async Task<IActionResult> SeedDatabase(CancellationToken ct = default)
	{
		try
		{
			await DbSeeder.SeedAsync(db, ct);
			return Ok(new { message = "Database seeded successfully" });
		}
		catch (Exception ex)
		{
			return BadRequest(new { message = $"Seeding failed: {ex.Message}" });
		}
	}
}
