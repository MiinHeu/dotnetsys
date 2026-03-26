using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.API.Services;

public static class DbSeeder
{
	public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
	{
		if (await db.AppUsers.AnyAsync(ct))
			return;

		var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@2026");
		var ownerHash = BCrypt.Net.BCrypt.HashPassword("Owner@2026");

		db.AppUsers.AddRange(
			new AppUser
			{
				Username = "admin",
				PasswordHash = adminHash,
				Role = "Admin",
				IsActive = true,
				OwnedPoiId = null,
				CreatedAt = DateTime.UtcNow
			},
			new AppUser
			{
				Username = "owner1",
				PasswordHash = ownerHash,
				Role = "Owner",
				IsActive = true,
				OwnedPoiId = 1,
				CreatedAt = DateTime.UtcNow
			});

		await db.SaveChangesAsync(ct);
	}
}
