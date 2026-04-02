namespace VinhKhanh.Infrastructure.Data;

public class Tour
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }

	public int EstimatedMinutes { get; set; } = 60;
	public string? ThumbnailUrl { get; set; }
	public bool IsActive { get; set; } = true;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public ICollection<TourStop> Stops { get; set; } = new List<TourStop>();
	public ICollection<TourTranslation> Translations { get; set; } = new List<TourTranslation>();
}

