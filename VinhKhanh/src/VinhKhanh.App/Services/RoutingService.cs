using System.Text.Json;

namespace VinhKhanh.App.Services;

public class RoutingService
{
    private readonly HttpClient _httpClient;

    public RoutingService()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Gọi OSRM API để lấy đường đi bộ (Walking Route) giữa các điểm.
    /// Trả về danh sách các tọa độ (Longitude, Latitude) dọc theo con đường thật.
    /// </summary>
    public async Task<List<(double Lon, double Lat)>?> GetWalkingRouteAsync(List<(double Lon, double Lat)> waypoints)
    {
        if (waypoints == null || waypoints.Count < 2)
            return null;

        try
        {
            // OSRM format: lon,lat;lon,lat
            var coordsStr = string.Join(";", waypoints.Select(w => $"{w.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{w.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}"));
            
            // Dùng server public OSRM (cho mục đích demo/PoC)
            var url = $"https://router.project-osrm.org/route/v1/foot/{coordsStr}?overview=full&geometries=geojson";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.GetProperty("code").GetString() == "Ok")
            {
                var routes = root.GetProperty("routes");
                if (routes.GetArrayLength() > 0)
                {
                    var geometry = routes[0].GetProperty("geometry");
                    var coordsArray = geometry.GetProperty("coordinates");
                    
                    var result = new List<(double Lon, double Lat)>();
                    foreach (var coord in coordsArray.EnumerateArray())
                    {
                        var lon = coord[0].GetDouble();
                        var lat = coord[1].GetDouble();
                        result.Add((lon, lat));
                    }
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RoutingService] Error fetching route: {ex.Message}");
        }

        return null;
    }
}
