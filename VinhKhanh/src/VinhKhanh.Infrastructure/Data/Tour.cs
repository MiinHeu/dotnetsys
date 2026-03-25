namespace VinhKhanh.Infrastructure.Data;

public class Tour
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }

	public ICollection<TourStop> TourStops { get; set; } = new List<TourStop>();
}

