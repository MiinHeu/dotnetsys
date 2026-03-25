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

	public Poi Poi { get; set; } = null!;
}

