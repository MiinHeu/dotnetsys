using System.ComponentModel.DataAnnotations;

namespace VinhKhanh.Shared.Models;

public class Poi
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OwnerInfo { get; set; }

    // GPS coordinates (outside, real-world).
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // Map coordinates (0-100) for UI rendering.
    public double MapX { get; set; }
    public double MapY { get; set; }

    // Geofence config.
    public double TriggerRadiusMeters { get; set; } = 15.0;
    public int CooldownSeconds { get; set; } = 60;
    public int Priority { get; set; } = 0;

    public string? ImageUrl { get; set; }
    public string? AudioViUrl { get; set; }

    public PoiCategory Category { get; set; } = PoiCategory.ComTam;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PoiTranslation> Translations { get; set; } = [];
    public ICollection<PoiVisitLog> VisitLogs { get; set; } = [];
}

public enum PoiCategory
{
    ComTam,
    BanhCanh,
    HaiSan,
    CheTrangMiem,
    DoUong,
    DacSan,
    DiemNhaTram,
}

public class PoiTranslation
{
    public int Id { get; set; }
    public int PoiId { get; set; }

    // ISO 639-1: vi, en, zh, ko, ja, fr, km, th...
    [MaxLength(16)]
    public string LanguageCode { get; set; } = "vi";

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }

    public Poi Poi { get; set; } = null!;
}

public class PoiVisitLog
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    [MaxLength(16)]
    public string LanguageCode { get; set; } = "vi";

    // "GPS" or "QR"
    [MaxLength(8)]
    public string TriggerType { get; set; } = "GPS";

    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
    public int ListenDurationSeconds { get; set; }

    public Poi Poi { get; set; } = null!;
}

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Admin/Owner (chu quan)
    public string Role { get; set; } = "Owner";
    public bool IsActive { get; set; } = true;
    public int? OwnedPoiId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
