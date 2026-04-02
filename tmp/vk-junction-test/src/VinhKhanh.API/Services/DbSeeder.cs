using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.API.Services;

public static class DbSeeder
{
	public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
	{
		var adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@2026");
		var ownerHash = BCrypt.Net.BCrypt.HashPassword("Owner@2026");

		var changed = false;

		var admin = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == "admin", ct);
		if (admin == null)
		{
			db.AppUsers.Add(new AppUser
			{
				Username = "admin",
				PasswordHash = adminHash,
				Role = "Admin",
				IsActive = true,
				OwnedPoiId = null,
				CreatedAt = DateTime.UtcNow
			});
			changed = true;
		}
		else
		{
			admin.PasswordHash = adminHash;
			admin.Role = "Admin";
			admin.IsActive = true;
			admin.OwnedPoiId = null;
			changed = true;
		}

		var owner = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == "owner1", ct);
		if (owner == null)
		{
			db.AppUsers.Add(new AppUser
			{
				Username = "owner1",
				PasswordHash = ownerHash,
				Role = "Owner",
				IsActive = true,
				OwnedPoiId = 1,
				CreatedAt = DateTime.UtcNow
			});
			changed = true;
		}
		else
		{
			owner.PasswordHash = ownerHash;
			owner.Role = "Owner";
			owner.IsActive = true;
			owner.OwnedPoiId = 1;
			changed = true;
		}

		if (changed)
			await db.SaveChangesAsync(ct);
	}
}
