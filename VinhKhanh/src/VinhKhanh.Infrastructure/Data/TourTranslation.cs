namespace VinhKhanh.Infrastructure.Data;

public class TourTranslation
{
	public int Id { get; set; }
	public int TourId { get; set; }

	public string LanguageCode { get; set; } = "vi";
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	public Tour Tour { get; set; } = null!;
}

