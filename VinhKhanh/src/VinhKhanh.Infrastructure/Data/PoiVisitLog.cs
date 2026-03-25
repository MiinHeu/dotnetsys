namespace VinhKhanh.Infrastructure.Data;

public class PoiVisitLog
{
	public int Id { get; set; }
	public int PoiId { get; set; }
	public string SessionId { get; set; } = Guid.NewGuid().ToString();

	public string LanguageCode { get; set; } = "vi";
	public string TriggerType { get; set; } = "GPS"; // GPS or QR
	public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
	public int ListenDurationSeconds { get; set; }

	public Poi Poi { get; set; } = null!;
}

