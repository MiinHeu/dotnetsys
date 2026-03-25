namespace VinhKhanh.API.Utilities;

public static class GeoMath
{
	// Radius of Earth in meters.
	private const double EarthRadiusMeters = 6371000;

	public static double Haversine(double lat1, double lon1, double lat2, double lon2)
	{
		var dLat = (lat2 - lat1) * Math.PI / 180.0;
		var dLon = (lon2 - lon1) * Math.PI / 180.0;

		var a =
			Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
			Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
			Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		return EarthRadiusMeters * c;
	}

	public static bool IsInGeofence(double userLat, double userLon, double poiLat, double poiLon, double radiusMeters)
		=> Haversine(userLat, userLon, poiLat, poiLon) <= radiusMeters;
}

