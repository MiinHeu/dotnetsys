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
		await SeedUsersAsync(db, forceDefaultCredentials, ct);
		await SeedSessionsAsync(db, ct);
	}

	private static async Task SeedSessionsAsync(ApplicationDbContext db, CancellationToken ct)
	{
		try
		{
			// Chỉ tạo dữ liệu mẫu nếu bảng đang trống
			if (await db.DeviceSessions.AnyAsync(ct)) return;
		}
		catch
		{
			// Bảng chưa tồn tại (chưa migration xong), bỏ qua để không sập App
			return;
		}

		var rnd = new Random();
		var platforms = new[] { "Android", "iOS" };
		var models = new[] { "iPhone 15 Pro", "Samsung S24 Ultra", "Pixel 8 Pro", "iPhone 13", "Xiaomi 14" };
		var manufacturers = new[] { "Apple", "Samsung", "Google", "Xiaomi" };
		var languages = new[] { "vi", "en", "ko", "zh" };

		for (int i = 0; i < 50; i++)
		{
			var startedAt = DateTime.UtcNow.AddDays(-rnd.Next(0, 30)).AddHours(-rnd.Next(0, 24));
			var duration = rnd.Next(10, 120);
			var platformIdx = rnd.Next(platforms.Length);

			db.DeviceSessions.Add(new DeviceSession
			{
				SessionId = Guid.NewGuid().ToString(),
				DeviceModel = models[rnd.Next(models.Length)],
				DevicePlatform = platforms[platformIdx],
				Manufacturer = manufacturers[platformIdx == 1 ? 0 : rnd.Next(1, manufacturers.Length)],
				OsVersion = platformIdx == 1 ? "17.4" : "14.0",
				AppVersion = "1.0.5",
				LanguageUsed = languages[rnd.Next(languages.Length)],
				StartedAt = startedAt,
				EndedAt = startedAt.AddMinutes(duration),
				LastHeartbeatAt = startedAt.AddMinutes(duration),
				PoisVisited = rnd.Next(1, 8),
				DistanceMeters = rnd.Next(200, 3500),
				IsReturning = rnd.Next(0, 10) > 7
			});
		}

		await db.SaveChangesAsync(ct);
	}


	private static async Task SeedUsersAsync(ApplicationDbContext db, bool forceDefaultCredentials, CancellationToken ct)
	{
		var changed = false;

		var admin = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == "admin", ct);
		if (admin == null)
		{
			db.AppUsers.Add(new AppUser
			{
				Username = "admin",
				DisplayId = "ADM-001",
				FullName = "Quản trị viên",
				Email = "admin@vinhkhanh.vn",
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
			if (string.IsNullOrEmpty(admin.DisplayId))
			{
				admin.DisplayId = "ADM-001";
				admin.FullName = "Quản trị viên";
				admin.Email = "admin@vinhkhanh.vn";
				changed = true;
			}
		}

		var owner = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == "owner1", ct);
		if (owner == null)
		{
			db.AppUsers.Add(new AppUser
			{
				Username = "owner1",
				DisplayId = "OW-001",
				FullName = "Chủ quán mẫu",
				Email = "owner1@vinhkhanh.vn",
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
			if (string.IsNullOrEmpty(owner.DisplayId))
			{
				owner.DisplayId = "OW-001";
				owner.FullName = "Chủ quán mẫu";
				owner.Email = "owner1@vinhkhanh.vn";
				changed = true;
			}
		}

		if (changed)
			await db.SaveChangesAsync(ct);
	}
}
