using Microsoft.EntityFrameworkCore;

namespace VinhKhanh.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

	public DbSet<Poi> Pois => Set<Poi>();
	public DbSet<Tour> Tours => Set<Tour>();
	public DbSet<TourStop> TourStops => Set<TourStop>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Keep table names stable to match your Step 6 expectation.
		modelBuilder.Entity<Poi>().ToTable("Pois");
		modelBuilder.Entity<Tour>().ToTable("Tours");
		modelBuilder.Entity<TourStop>().ToTable("TourStops");

		modelBuilder.Entity<Poi>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Name).HasMaxLength(250).IsRequired();
		});

		modelBuilder.Entity<Tour>(entity =>
		{
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Title).HasMaxLength(250).IsRequired();
		});

		modelBuilder.Entity<TourStop>(entity =>
		{
			entity.HasKey(x => x.Id);

			entity.Property(x => x.StopOrder).IsRequired();

			entity.HasOne(x => x.Tour)
				.WithMany(x => x.TourStops)
				.HasForeignKey(x => x.TourId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.Poi)
				.WithMany(x => x.TourStops)
				.HasForeignKey(x => x.PoiId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasIndex(x => new { x.TourId, x.StopOrder }).IsUnique();
		});
	}
}

