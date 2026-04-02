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

	public Task<List<Poi>> CheckTriggeredAsync(Location loc, List<Poi> pois)
	{
		var radiusMultiplier = GetRadiusMultiplier();

		var triggered = new List<Poi>();
		var now = DateTime.UtcNow;

		foreach (var poi in pois.OrderByDescending(p => p.Priority))
		{
			var dist = GeoMath.Haversine(loc.Latitude, loc.Longitude, poi.Latitude, poi.Longitude);
			var adjustedRadius = poi.TriggerRadiusMeters * radiusMultiplier;
			var exitRadius = adjustedRadius * ExitHysteresisFactor;

			// Hysteresis: once inside, only mark exit when farther than expanded radius.
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

			if (_lastTriggered.TryGetValue(poi.Id, out var last)
				&& (now - last).TotalSeconds < poi.CooldownSeconds)
				continue;

			_lastTriggered[poi.Id] = now;
			_insidePoiIds.Add(poi.Id);
			triggered.Add(poi);
		}

		return Task.FromResult(triggered);
	}

	private static double GetRadiusMultiplier()
	{
		var raw = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.GpsRadiusMultiplier, "1");
		return double.TryParse(raw, out var value) && value > 0 ? value : 1d;
	}
}
