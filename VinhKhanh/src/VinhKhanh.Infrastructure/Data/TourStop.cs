namespace VinhKhanh.Infrastructure.Data;

public class TourStop
{
	public int Id { get; set; }

	public int TourId { get; set; }
	public int PoiId { get; set; }

	// Used to keep an ordered route within the same tour.
	public int StopOrder { get; set; }

	public Tour? Tour { get; set; }
	public Poi? Poi { get; set; }
}

