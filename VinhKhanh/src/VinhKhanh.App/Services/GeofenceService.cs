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
	private const double ExitHysteresisFactor = 1.5; // Tăng vùng đệm thoát để tránh GPS jitter
	private DateTime _lastGlobalTrigger = DateTime.MinValue;
	private const int GlobalCooldownSeconds = 10; // Khoảng nghỉ giữa 2 lần kích hoạt khác nhau

	public Task<List<Poi>> CheckTriggeredAsync(Location loc, List<Poi> pois)
	{
		var radiusMultiplier = GetRadiusMultiplier();
		var now = DateTime.UtcNow;

		// Bỏ qua nếu vừa kích hoạt một POI bất kỳ gần đây (Global Cooldown)
		if ((now - _lastGlobalTrigger).TotalSeconds < GlobalCooldownSeconds)
			return Task.FromResult(new List<Poi>());

		var triggeredWithDist = new List<(Poi poi, double dist)>();
		foreach (var poi in pois)
		{
			var dist = VinhKhanh.Shared.GeoMath.Haversine(loc.Latitude, loc.Longitude, poi.Latitude, poi.Longitude);
			var adjustedRadius = poi.TriggerRadiusMeters * radiusMultiplier;
			var exitRadius = adjustedRadius * ExitHysteresisFactor;

			var isInside = _insidePoiIds.Contains(poi.Id);

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

			_consecutiveHits[poi.Id] = _consecutiveHits.TryGetValue(poi.Id, out var n) ? n + 1 : 1;
			if (_consecutiveHits[poi.Id] < RequiredConsecutiveHits) continue;

			if (_lastTriggered.TryGetValue(poi.Id, out var last) && (now - last).TotalSeconds < poi.CooldownSeconds)
				continue;

			_insidePoiIds.Add(poi.Id);
			_lastTriggered[poi.Id] = now;
			triggeredWithDist.Add((poi, dist));
		}

		if (triggeredWithDist.Count > 0)
		{
			_lastGlobalTrigger = now;
		}

		// Sắp xếp: Ưu tiên cao nhất -> Gần nhất
		return Task.FromResult(triggeredWithDist
			.OrderByDescending(x => x.poi.Priority)
			.ThenBy(x => x.dist)
			.Select(x => x.poi)
			.ToList());
	}

	private static double GetRadiusMultiplier()
	{
		var raw = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.GpsRadiusMultiplier, "1");
		return double.TryParse(raw, out var value) && value > 0 ? value : 1d;
	}
}
