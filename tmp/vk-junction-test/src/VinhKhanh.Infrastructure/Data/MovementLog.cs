namespace VinhKhanh.Infrastructure.Data;

public class MovementLog
{
	public long Id { get; set; }
	public string SessionId { get; set; } = string.Empty;
	public double Latitude { get; set; }
	public double Longitude { get; set; }
	public float AccuracyMeters { get; set; }
	public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

