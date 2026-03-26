using System.Text.Json.Serialization;

namespace VinhKhanh.App.Models;

public sealed class TourSnapshot
{
	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("description")] public string? Description { get; set; }
	[JsonPropertyName("estimatedMinutes")] public int EstimatedMinutes { get; set; }
	[JsonPropertyName("stops")] public List<TourStopSnapshot>? Stops { get; set; }
}

public sealed class TourStopSnapshot
{
	[JsonPropertyName("stopOrder")] public int StopOrder { get; set; }
	[JsonPropertyName("stayMinutes")] public int StayMinutes { get; set; }
	[JsonPropertyName("poi")] public PoiSnapshot? Poi { get; set; }
}
