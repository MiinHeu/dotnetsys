using Microsoft.EntityFrameworkCore;

namespace VinhKhanh.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

	public DbSet<Poi> Pois => Set<Poi>();
	public DbSet<PoiTranslation> PoiTranslations => Set<PoiTranslation>();
	public DbSet<PoiVisitLog> PoiVisitLogs => Set<PoiVisitLog>();
	public DbSet<AppUser> AppUsers => Set<AppUser>();

	public DbSet<Tour> Tours => Set<Tour>();
	public DbSet<TourTranslation> TourTranslations => Set<TourTranslation>();

	public DbSet<TourStop> TourStops => Set<TourStop>();

	public DbSet<MovementLog> MovementLogs => Set<MovementLog>();
	public DbSet<AppHistoryLog> AppHistoryLogs => Set<AppHistoryLog>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Keep table names stable to match your Step 6 expectation.
		modelBuilder.Entity<Poi>().ToTable("Pois");
		modelBuilder.Entity<PoiTranslation>().ToTable("PoiTranslations");
		modelBuilder.Entity<PoiVisitLog>().ToTable("PoiVisitLogs");
		modelBuilder.Entity<AppUser>().ToTable("AppUsers");

		modelBuilder.Entity<Tour>().ToTable("Tours");
		modelBuilder.Entity<TourTranslation>().ToTable("TourTranslations");
		modelBuilder.Entity<TourStop>().ToTable("TourStops");

		modelBuilder.Entity<MovementLog>().ToTable("MovementLogs");
		modelBuilder.Entity<AppHistoryLog>().ToTable("AppHistoryLogs");

		modelBuilder.Entity<Poi>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
			entity.Property(x => x.Description).IsRequired();
			entity.Property(x => x.QrCode).HasMaxLength(64);
			entity.HasIndex(x => x.ContentVersion);

			entity.HasQueryFilter(p => p.IsActive);
			entity.HasIndex(p => new { p.Latitude, p.Longitude });
			entity.HasIndex(p => p.Category);
			entity.HasIndex(p => p.QrCode).IsUnique().HasFilter("\"QrCode\" IS NOT NULL");
		});

		modelBuilder.Entity<PoiTranslation>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
			entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
			entity.Property(x => x.Description).IsRequired();

			// Match Poi query filter so filtered-out Pois won't cause unexpected required-relationship behavior.
			entity.HasQueryFilter(t => t.Poi.IsActive);

			entity.HasOne(x => x.Poi)
				.WithMany(x => x.Translations)
				.HasForeignKey(x => x.PoiId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasIndex(t => new { t.PoiId, t.LanguageCode }).IsUnique();
		});

		modelBuilder.Entity<PoiVisitLog>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.Property(x => x.SessionId).IsRequired();
			entity.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
			entity.Property(x => x.TriggerType).HasMaxLength(32).IsRequired();
			entity.HasIndex(v => v.VisitedAt);

			// Match Poi query filter.
			entity.HasQueryFilter(v => v.Poi.IsActive);

			entity.HasOne(x => x.Poi)
				.WithMany(x => x.VisitLogs)
				.HasForeignKey(x => x.PoiId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<AppUser>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Username).IsRequired();
			entity.Property(x => x.PasswordHash).IsRequired();
			entity.Property(x => x.Role).IsRequired();
			entity.HasIndex(x => x.Username).IsUnique();
		});

		modelBuilder.Entity<Tour>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
			entity.Property(x => x.Description);
		});

		modelBuilder.Entity<TourTranslation>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
			entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
			entity.Property(x => x.Description).IsRequired();

			entity.HasOne(x => x.Tour)
				.WithMany(x => x.Translations)
				.HasForeignKey(x => x.TourId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<TourStop>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.Property(x => x.StopOrder).IsRequired();
			entity.Property(x => x.StayMinutes).IsRequired();

			// Match Poi query filter so TourStops won't surface for inactive Pois via required relationships.
			entity.HasQueryFilter(ts => ts.Poi.IsActive);

			entity.HasOne(x => x.Tour)
				.WithMany(x => x.Stops)
				.HasForeignKey(x => x.TourId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.Poi)
				.WithMany(x => x.TourStops)
				.HasForeignKey(x => x.PoiId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasIndex(x => new { x.TourId, x.StopOrder }).IsUnique();
		});

		modelBuilder.Entity<MovementLog>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => x.RecordedAt);
			entity.HasIndex(x => x.SessionId);
		});

		modelBuilder.Entity<AppHistoryLog>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.HasIndex(x => x.CreatedAt);
			entity.HasIndex(x => x.EventType);
		});

		// Seed minimal demo data (refine later).
		var seedTime = new DateTime(2026, 03, 25, 0, 0, 0, DateTimeKind.Utc);
		modelBuilder.Entity<Poi>().HasData(
			new Poi
			{
				Id = 1,
				Name = "Quan Com Tam Ba Ghien",
				Description = "Com tam dac trung Sai Gon 30 nam.",
				OwnerInfo = null,
				Latitude = 10.7531,
				Longitude = 106.6780,
				MapX = 15,
				MapY = 40,
				TriggerRadiusMeters = 15,
				Priority = 9,
				CooldownSeconds = 60,
				Category = PoiCategory.ComTam,
				ImageUrl = null,
				AudioViUrl = null,
				QrCode = "VK-POI-001",
				ContentVersion = 1,
				IsActive = true,
				CreatedAt = seedTime,
				UpdatedAt = seedTime
			},
			new Poi
			{
				Id = 2,
				Name = "Banh Canh Cua Ba Suong",
				Description = "Banh canh cua tuoi boc day, 40 nam.",
				OwnerInfo = null,
				Latitude = 10.7533,
				Longitude = 106.6781,
				MapX = 30,
				MapY = 40,
				TriggerRadiusMeters = 15,
				Priority = 8,
				CooldownSeconds = 60,
				Category = PoiCategory.BanhCanh,
				ImageUrl = null,
				AudioViUrl = null,
				QrCode = "VK-POI-002",
				ContentVersion = 1,
				IsActive = true,
				CreatedAt = seedTime,
				UpdatedAt = seedTime
			},
			new Poi
			{
				Id = 3,
				Name = "Khu Che Cuoi Pho",
				Description = "Khu vuc tap trung hang che.",
				OwnerInfo = null,
				Latitude = 10.7540,
				Longitude = 106.6785,
				MapX = 75,
				MapY = 40,
				TriggerRadiusMeters = 20,
				Priority = 5,
				CooldownSeconds = 120,
				Category = PoiCategory.CheTrangMiem,
				ImageUrl = null,
				AudioViUrl = null,
				QrCode = "VK-POI-003",
				ContentVersion = 1,
				IsActive = true,
				CreatedAt = seedTime,
				UpdatedAt = seedTime
			}
		);

		modelBuilder.Entity<Tour>().HasData(
			new Tour
			{
				Id = 1,
				Name = "Tour Am Thuc 1 Gio Vinh Khanh",
				Description = "Com tam -> Banh canh -> Che",
				EstimatedMinutes = 60,
				ThumbnailUrl = null,
				IsActive = true,
				CreatedAt = seedTime,
				UpdatedAt = seedTime
			}
		);

		modelBuilder.Entity<TourStop>().HasData(
			new TourStop { Id = 1, TourId = 1, PoiId = 1, StopOrder = 1, StayMinutes = 20, Note = null },
			new TourStop { Id = 2, TourId = 1, PoiId = 2, StopOrder = 2, StayMinutes = 20, Note = null },
			new TourStop { Id = 3, TourId = 1, PoiId = 3, StopOrder = 3, StayMinutes = 20, Note = null }
		);
	}
}

