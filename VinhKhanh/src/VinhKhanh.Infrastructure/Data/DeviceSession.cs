namespace VinhKhanh.Infrastructure.Data;

/// <summary>
/// Lưu trữ thông tin mỗi phiên sử dụng App của du khách.
/// Ghi nhận thiết bị, thời gian, và hành vi tham quan.
/// </summary>
public class DeviceSession
{
	public long Id { get; set; }

	/// <summary>Liên kết với PoiVisitLog, MovementLog, AppHistoryLog qua cùng SessionId.</summary>
	public string SessionId { get; set; } = string.Empty;

	// ── Thông tin thiết bị ──
	public string DeviceModel { get; set; } = string.Empty;        // "Samsung Galaxy S24", "iPhone 15 Pro"
	public string DevicePlatform { get; set; } = string.Empty;     // "Android", "iOS"
	public string OsVersion { get; set; } = string.Empty;          // "14.0", "17.4"
	public string AppVersion { get; set; } = string.Empty;         // "1.0.0"
	public string Manufacturer { get; set; } = string.Empty;       // "Samsung", "Apple", "Xiaomi"

	// ── Thời gian phiên ──
	public DateTime StartedAt { get; set; } = DateTime.UtcNow;
	public DateTime LastHeartbeatAt { get; set; } = DateTime.UtcNow;
	public DateTime? EndedAt { get; set; }

	// ── Thống kê hành vi ──
	public int PoisVisited { get; set; }
	public double DistanceMeters { get; set; }
	public string LanguageUsed { get; set; } = "vi";

	/// <summary>Du khách này đã từng dùng App trước đó (SessionId cũ tồn tại).</summary>
	public bool IsReturning { get; set; }

	// ── Thông tin vị trí từ IP ──
	public string? IpAddress { get; set; }
	public string? Country { get; set; }
	public string? City { get; set; }

	/// <summary>0: Mạnh, 1: Yếu - Được xác định khi tải App.</summary>
	public int? ConfigurationLevel { get; set; }
}
