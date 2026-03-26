using Microsoft.Maui.Devices.Sensors;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared;

namespace VinhKhanh.App.Services;

public class GeofenceService : IGeofenceService
{
	private readonly Dictionary<int, DateTime> _lastTriggered = new();

	public Task<List<Poi>> CheckTriggeredAsync(Location loc, List<Poi> pois)
	{
		// FIX #23: Đọc GpsRadiusMultiplier từ Preferences
		var radiusMultiplier = Microsoft.Maui.Storage.Preferences.Get("gps_radius_multiplier", 1.0f);

		var triggered = new List<Poi>();
		var now = DateTime.UtcNow;

		foreach (var poi in pois.OrderByDescending(p => p.Priority))
		{
			var dist = GeoMath.Haversine(loc.Latitude, loc.Longitude, poi.Latitude, poi.Longitude);
			var adjustedRadius = poi.TriggerRadiusMeters * radiusMultiplier;

			if (dist > adjustedRadius) continue;

			if (_lastTriggered.TryGetValue(poi.Id, out var last)
				&& (now - last).TotalSeconds < poi.CooldownSeconds)
				continue;

			_lastTriggered[poi.Id] = now;
			triggered.Add(poi);
		}

		return Task.FromResult(triggered);
	}
}
