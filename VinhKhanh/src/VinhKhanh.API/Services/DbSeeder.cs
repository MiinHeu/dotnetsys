using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.API.Services;

public static class DbSeeder
{
	private const string DefaultAdminPassword = "Admin@2026";
	private const string DefaultOwnerPassword = "Owner@2026";

	public static async Task SeedAsync(
		ApplicationDbContext db,
		bool forceDefaultCredentials = false,
		CancellationToken ct = default)
	{
		var changed = false;

		var admin = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == "admin", ct);
		if (admin == null)
		{
			db.AppUsers.Add(new AppUser
			{
				Username = "admin",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword),
				Role = "Admin",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			});
			changed = true;
		}
		else
		{
			if (admin.Role != "Admin")
			{
				admin.Role = "Admin";
				changed = true;
			}
			if (!admin.IsActive)
			{
				admin.IsActive = true;
				changed = true;
			}
			if (forceDefaultCredentials && !BCrypt.Net.BCrypt.Verify(DefaultAdminPassword, admin.PasswordHash))
			{
				admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword);
				changed = true;
			}
		}

		var owner = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == "owner1", ct);
		if (owner == null)
		{
			db.AppUsers.Add(new AppUser
			{
				Username = "owner1",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultOwnerPassword),
				Role = "Owner",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			});
			changed = true;
		}
		else
		{
			if (owner.Role != "Owner")
			{
				owner.Role = "Owner";
				changed = true;
			}
			if (!owner.IsActive)
			{
				owner.IsActive = true;
				changed = true;
			}
			if (forceDefaultCredentials && !BCrypt.Net.BCrypt.Verify(DefaultOwnerPassword, owner.PasswordHash))
			{
				owner.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultOwnerPassword);
				changed = true;
			}
		}

		if (changed)
			await db.SaveChangesAsync(ct);
	}
}
