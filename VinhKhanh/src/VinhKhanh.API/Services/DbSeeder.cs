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
		await SeedAnalyticsAsync(db, ct);
	}

	private static async Task SeedAnalyticsAsync(ApplicationDbContext db, CancellationToken ct)
	{
		// Only seed if empty to avoid bloat
		if (await db.MovementLogs.AnyAsync(ct)) return;

		Console.WriteLine("[SEED] Generating Heatmap and Analytics data...");
		var random = new Random();
		var now = DateTime.UtcNow;

		// 1. Seed Movement Logs for Heatmap (approx 200 points around Vinh Khanh street)
		// Vinh Khanh street is roughly around Lat: 10.7535, Lng: 106.6782
		var movements = new List<MovementLog>();
		for (int i = 0; i < 200; i++)
		{
			movements.Add(new MovementLog
			{
				SessionId = $"seed-session-{random.Next(1, 20)}",
				Latitude = 10.7535 + (random.NextDouble() - 0.5) * 0.002,
				Longitude = 106.6782 + (random.NextDouble() - 0.5) * 0.002,
				AccuracyMeters = (float)random.Next(5, 20),
				RecordedAt = now.AddHours(-random.Next(1, 48))
			});
		}
		db.MovementLogs.AddRange(movements);

		// 2. Seed Visit Logs for Top Analytics (approx 50 visits)
		// We know POIs 1 to 5 exist from ApplicationDbContext HasData
		var visits = new List<PoiVisitLog>();
		for (int i = 0; i < 50; i++)
		{
			visits.Add(new PoiVisitLog
			{
				PoiId = random.Next(1, 6),
				SessionId = $"seed-user-{random.Next(1, 50)}",
				LanguageCode = i % 5 == 0 ? "en" : "vi",
				TriggerType = i % 3 == 0 ? "QR" : "GPS",
				ListenDurationSeconds = random.Next(10, 300),
				VisitedAt = now.AddDays(-random.Next(0, 7))
			});
		}
		db.PoiVisitLogs.AddRange(visits);

		await db.SaveChangesAsync(ct);
		Console.WriteLine("[SEED] Analytics data seeded successfully.");
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
