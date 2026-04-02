using VinhKhanh.App.Models;
using VinhKhanh.Shared;

namespace VinhKhanh.App.Services;

public static class GeofenceEvaluator
{
	public static PoiSnapshot? PickBestInside(double lat, double lon, IReadOnlyList<PoiSnapshot> pois, double radiusMultiplier = 1.0)
	{
		PoiSnapshot? best = null;
		var bestScore = double.MinValue;

		foreach (var p in pois)
		{
			var dist = GeoMath.Haversine(lat, lon, p.Latitude, p.Longitude);
			var r = p.TriggerRadiusMeters * radiusMultiplier;
			if (dist > r) continue;

			var score = p.Priority * 1_000_000.0 - dist;
			if (score > bestScore)
			{
				bestScore = score;
				best = p;
			}
		}

		return best;
	}
}
