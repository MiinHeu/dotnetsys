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

	public int? OwnerUserId { get; set; }

	public PoiCategory Category { get; set; } = PoiCategory.ComTam;
	public bool IsActive { get; set; } = true;

	// ── Dữ liệu mở rộng cho Mobile V2.0 ──
	public string? Address { get; set; }
	public string? PhoneNumber { get; set; }
	public string? OperatingHours { get; set; } // Vd: "15:00 - 23:00"
	public double Rating { get; set; } = 5.0;   // Đánh giá trung bình
	
	/// <summary>Lưu mảng URL ảnh dạng JSON ["url1", "url2"]</summary>
	public string? ImagesJson { get; set; }     
	
	/// <summary>Lưu danh sách món ăn dạng JSON [{"name":"Ốc", "price":50000}]</summary>
	public string? MenuJson { get; set; }       
	
	/// <summary>Lưu thẻ phân loại dạng JSON ["Hải sản", "Ăn vặt"]</summary>
	public string? TagsJson { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public AppUser? Owner { get; set; }
	public ICollection<PoiTranslation> Translations { get; set; } = new List<PoiTranslation>();
	public ICollection<PoiVisitLog> VisitLogs { get; set; } = new List<PoiVisitLog>();
	public ICollection<TourStop> TourStops { get; set; } = new List<TourStop>();

	public int CompareTo(Poi? other) => other == null ? 1 : other.Priority.CompareTo(Priority);
}

