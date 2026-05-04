using Microsoft.Maui.Devices.Sensors;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared;

namespace VinhKhanh.App.Services;

public class GeofenceService : IGeofenceService
{
	private readonly Dictionary<int, DateTime> _lastTriggered = new();
	private readonly Dictionary<int, int> _consecutiveHits = new();
	private readonly HashSet<int> _insidePoiIds = [];
	private const int RequiredConsecutiveHits = 2;
	private const double ExitHysteresisFactor = 1.2;
	private DateTime _lastGlobalTrigger = DateTime.MinValue;
	private const int GlobalCooldownSeconds = 10; // Khoảng nghỉ giữa 2 lần kích hoạt khác nhau

	public Task<List<Poi>> CheckTriggeredAsync(Location loc, List<Poi> pois)
	{
		var radiusMultiplier = GetRadiusMultiplier();
		var now = DateTime.UtcNow;

		// Bỏ qua nếu vừa kích hoạt một POI bất kỳ gần đây (Global Cooldown)
		if ((now - _lastGlobalTrigger).TotalSeconds < GlobalCooldownSeconds)
			return Task.FromResult(new List<Poi>());

		Poi? bestPoi = null;
		double bestDistance = double.MaxValue;

		foreach (var poi in pois)
		{
			var dist = GeoMath.Haversine(loc.Latitude, loc.Longitude, poi.Latitude, poi.Longitude);
			var adjustedRadius = poi.TriggerRadiusMeters * radiusMultiplier;
			var exitRadius = adjustedRadius * ExitHysteresisFactor;

			var isInside = _insidePoiIds.Contains(poi.Id);

			// Logic Hysteresis: tránh jitter tại biên
			if (!isInside && dist > adjustedRadius)
			{
				_consecutiveHits[poi.Id] = 0;
				continue;
			}
			if (isInside && dist > exitRadius)
			{
				_insidePoiIds.Remove(poi.Id);
				_consecutiveHits[poi.Id] = 0;
				continue;
			}

			// Đã lọt vào vùng kích hoạt
			_consecutiveHits[poi.Id] = _consecutiveHits.TryGetValue(poi.Id, out var n) ? n + 1 : 1;
			if (_consecutiveHits[poi.Id] < RequiredConsecutiveHits) continue;

			// Kiểm tra Cooldown
			if (_lastTriggered.TryGetValue(poi.Id, out var last) && (now - last).TotalSeconds < poi.CooldownSeconds)
				continue;

			// Lưu trạng thái 'đang ở trong'
			_insidePoiIds.Add(poi.Id);

			// Xử lý xung đột (Overlap): Chọn POI tốt nhất dựa trên Priority và Distance
			if (bestPoi == null || poi.Priority > bestPoi.Priority || (poi.Priority == bestPoi.Priority && dist < bestDistance))
			{
				bestPoi = poi;
				bestDistance = dist;
			}
		}

		var triggered = new List<Poi>();
		if (bestPoi != null)
		{
			_lastTriggered[bestPoi.Id] = now;
			_lastGlobalTrigger = now; // Cập nhật mốc thời gian kích hoạt toàn cục
			triggered.Add(bestPoi);
		}

		return Task.FromResult(triggered);
	}

	private static double GetRadiusMultiplier()
	{
		var raw = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.GpsRadiusMultiplier, "1");
		return double.TryParse(raw, out var value) && value > 0 ? value : 1d;
	}
}
