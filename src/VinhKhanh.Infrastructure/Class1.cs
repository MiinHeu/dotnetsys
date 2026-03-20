using Microsoft.EntityFrameworkCore;
using VinhKhanh.Shared.Models;

namespace VinhKhanh.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Poi> Pois => Set<Poi>();
    public DbSet<PoiTranslation> PoiTranslations => Set<PoiTranslation>();
    public DbSet<PoiVisitLog> PoiVisitLogs => Set<PoiVisitLog>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        // Soft delete: only active POI are visible.
        m.Entity<Poi>().HasQueryFilter(p => p.IsActive);

        // Indexes for faster nearby queries.
        m.Entity<Poi>().HasIndex(p => new { p.Latitude, p.Longitude });
        m.Entity<Poi>().HasIndex(p => p.Category);

        // Translation uniqueness (PoiId + LanguageCode).
        m.Entity<PoiTranslation>()
            .HasIndex(t => new { t.PoiId, t.LanguageCode })
            .IsUnique();

        // Analytics indexes.
        m.Entity<PoiVisitLog>().HasIndex(v => v.VisitedAt);
        m.Entity<PoiVisitLog>().HasIndex(v => v.PoiId);

        // Seed data for PoC (sample POIs on Vinh Khanh food street).
        // Note: keep seed values deterministic so EF Core doesn't complain about
        // PendingModelChangesWarning during database update.
        var seedAt = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc);

        m.Entity<Poi>().HasData(
            new Poi
            {
                Id = 1,
                Name = "Quan Com Tam Ba Ghien",
                Latitude = 10.7531,
                Longitude = 106.6780,
                MapX = 15,
                MapY = 40,
                TriggerRadiusMeters = 15,
                Priority = 9,
                CooldownSeconds = 60,
                Category = PoiCategory.ComTam,
                Description = "Com tam dac trung Sai Gon 30 nam. Suon nuong thom lung, bi ro gion, chan ga beo ngay.",
                CreatedAt = seedAt,
                UpdatedAt = seedAt
            },
            new Poi
            {
                Id = 2,
                Name = "Banh Canh Cua Ba Suong",
                Latitude = 10.7533,
                Longitude = 106.6781,
                MapX = 30,
                MapY = 40,
                TriggerRadiusMeters = 15,
                Priority = 8,
                CooldownSeconds = 60,
                Category = PoiCategory.BanhCanh,
                Description = "Banh canh cua tuoi boc day, nuoc leo ngot thanh, gan 40 nam phuc vu.",
                CreatedAt = seedAt,
                UpdatedAt = seedAt
            },
            new Poi
            {
                Id = 3,
                Name = "Khu Che Cuoi Pho",
                Latitude = 10.7540,
                Longitude = 106.6785,
                MapX = 75,
                MapY = 40,
                TriggerRadiusMeters = 20,
                Priority = 5,
                CooldownSeconds = 120,
                Category = PoiCategory.CheTrangMiem,
                Description = "Khu vuc tap trung hang che, trang miem, banh ngot — ly tuong sau bua an.",
                CreatedAt = seedAt,
                UpdatedAt = seedAt
            }
        );
    }
}
