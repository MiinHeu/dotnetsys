namespace VinhKhanh.Infrastructure.Data;

public class TourStop
{
	public int Id { get; set; }

	public int TourId { get; set; }
	public int PoiId { get; set; }

	// Used to keep an ordered route within the same tour.
	public int StopOrder { get; set; }

	public int StayMinutes { get; set; } = 10;
	public string? Note { get; set; }

	public Tour Tour { get; set; } = null!;
	public Poi Poi { get; set; } = null!;
}

