namespace VinhKhanh.Infrastructure.Data;

public class AppHistoryLog
{
	public long Id { get; set; }
	public string SessionId { get; set; } = string.Empty;

	// GPS_TRIGGER, QR_SCAN, AI_CHAT, TTS_PLAY, AUDIO_PLAY
	public string EventType { get; set; } = string.Empty;

	public int? PoiId { get; set; }
	public int? TourId { get; set; }

	public string LanguageCode { get; set; } = "vi";
	public string? Payload { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

