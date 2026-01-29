// ============================================
// DOMAIN MODELS - Core Business Logic
// ============================================

using System;
using System.Collections.Generic;

namespace VinhKhanhNarration.Domain.Entities
{
    /// <summary>
    /// Điểm quan tâm trong phố ẩm thực (Point of Interest)
    /// </summary>
    public class POI
    {
        public Guid Id { get; set; }
        public string Code { get; set; } // VK001, VK002...
        public POIType Type { get; set; }
        public string Name { get; set; }
        public GeoLocation Location { get; set; }
        public List<Content> Contents { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Business logic: Tính khoảng cách đến khách
        public double DistanceTo(GeoLocation visitorLocation)
        {
            return Location.DistanceTo(visitorLocation);
        }
        
        // Lấy nội dung theo ngôn ngữ ưu tiên
        public Content GetContent(Language language, ContentType type = ContentType.Audio)
        {
            var content = Contents.Find(c => 
                c.Language == language && 
                c.Type == type && 
                c.IsActive);
            
            // Fallback sang tiếng Việt nếu không có
            return content ?? Contents.Find(c => 
                c.Language == Language.Vietnamese && 
                c.Type == type);
        }
    }

    public enum POIType
    {
        Restaurant,      // Nhà hàng
        FoodStall,       // Quầy ăn
        Landmark,        // Điểm đánh dấu
        Entrance,        // Cổng vào
        RestArea,        // Khu vực nghỉ
        Cultural,        // Điểm văn hóa
        Historical       // Lịch sử
    }

    /// <summary>
    /// Vị trí địa lý với độ chính xác cao
    /// </summary>
    public class GeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; } // Độ cao (nếu có nhiều tầng)
        
        // Tính khoảng cách Haversine (mét)
        public double DistanceTo(GeoLocation other)
        {
            const double R = 6371e3; // Bán kính Trái Đất (m)
            var φ1 = Latitude * Math.PI / 180;
            var φ2 = other.Latitude * Math.PI / 180;
            var Δφ = (other.Latitude - Latitude) * Math.PI / 180;
            var Δλ = (other.Longitude - Longitude) * Math.PI / 180;

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
    }

    /// <summary>
    /// Nội dung thuyết minh đa ngôn ngữ
    /// </summary>
    public class Content
    {
        public Guid Id { get; set; }
        public Language Language { get; set; }
        public ContentType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AudioUrl { get; set; }
        public string VideoUrl { get; set; }
        public int Duration { get; set; } // Giây
        public bool IsActive { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public enum Language
    {
        Vietnamese,
        English,
        Chinese,
        Korean,
        Japanese,
        French,
        Thai
    }

    public enum ContentType
    {
        Audio,
        Video,
        Text,
        Interactive
    }

    /// <summary>
    /// Khách tham quan với thiết bị
    /// </summary>
    public class Visitor
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; }
        public Language PreferredLanguage { get; set; }
        public GeoLocation CurrentLocation { get; set; }
        public List<VisitLog> VisitHistory { get; set; } = new();
        public DateTime LastActivity { get; set; }
        
        // Phát hiện POI gần nhất
        public POI FindNearestPOI(List<POI> allPOIs, double maxDistance = 10.0)
        {
            POI nearest = null;
            double minDistance = double.MaxValue;

            foreach (var poi in allPOIs)
            {
                if (!poi.IsActive) continue;
                
                var distance = CurrentLocation.DistanceTo(poi.Location);
                if (distance <= maxDistance && distance < minDistance)
                {
                    minDistance = distance;
                    nearest = poi;
                }
            }

            return nearest;
        }
    }

    public class VisitLog
    {
        public Guid POIId { get; set; }
        public DateTime VisitedAt { get; set; }
        public int DurationSeconds { get; set; }
        public bool ContentPlayed { get; set; }
    }
}

// ============================================
// DOMAIN SERVICES - Business Rules
// ============================================

namespace VinhKhanhNarration.Domain.Services
{
    public interface INarrationEngine
    {
        NarrationResult TriggerNarration(Guid visitorId, GeoLocation location);
        void UpdateVisitorLocation(Guid visitorId, GeoLocation location);
        void SetPreferredLanguage(Guid visitorId, Language language);
    }

    public class NarrationResult
    {
        public bool ShouldPlayNarration { get; set; }
        public POI TargetPOI { get; set; }
        public Content Content { get; set; }
        public string Message { get; set; }
        public NarrationTrigger TriggerReason { get; set; }
    }

    public enum NarrationTrigger
    {
        ProximityDetected,
        ManualRequest,
        ScheduledEvent,
        FirstVisit
    }
}
