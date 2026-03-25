namespace VinhKhanh.Infrastructure.Data;

public class Poi
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;

	// Optional metadata for future map features.
	public string? Description { get; set; }
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }

	public ICollection<TourStop> TourStops { get; set; } = new List<TourStop>();
}

