namespace VinhKhanh.Infrastructure.Data;

public enum PoiCategory
{
	ComTam,
	BanhCanh,
	HaiSan,
	CheTrangMiem,
	DoUong,
	DacSan,
	DiemNhaTram
}

public class Poi : IComparable<Poi>
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string? OwnerInfo { get; set; }

	public double Latitude { get; set; }
	public double Longitude { get; set; }

	// 0-100% grid display
	public double MapX { get; set; }
	public double MapY { get; set; }

	public double TriggerRadiusMeters { get; set; } = 15.0;
	public int CooldownSeconds { get; set; } = 60;
	public int Priority { get; set; } = 0;

	public string? ImageUrl { get; set; }
	public string? AudioViUrl { get; set; }
	public string? QrCode { get; set; }
	public int ContentVersion { get; set; } = 1;

	public PoiCategory Category { get; set; } = PoiCategory.ComTam;
	public bool IsActive { get; set; } = true;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public ICollection<PoiTranslation> Translations { get; set; } = new List<PoiTranslation>();
	public ICollection<PoiVisitLog> VisitLogs { get; set; } = new List<PoiVisitLog>();
	public ICollection<TourStop> TourStops { get; set; } = new List<TourStop>();

	public int CompareTo(Poi? other) => other == null ? 1 : other.Priority.CompareTo(Priority);
}

