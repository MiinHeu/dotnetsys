namespace VinhKhanh.Infrastructure.Data;

public class PoiTranslation
{
	public int Id { get; set; }
	public int PoiId { get; set; }

	// vi, en, zh, ko, ja, km, th...
	public string LanguageCode { get; set; } = "vi";
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string? AudioUrl { get; set; }

	// Lưu nội dung description gốc (tiếng Việt) tại thời điểm dịch
	// Để so sánh khi description gốc thay đổi
	public string OriginalDescription { get; set; } = string.Empty;

	[System.Text.Json.Serialization.JsonIgnore]
	public Poi Poi { get; set; } = null!;
}

