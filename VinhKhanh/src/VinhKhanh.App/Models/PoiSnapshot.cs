using System.Text.Json.Serialization;

namespace VinhKhanh.App.Models;

public sealed class PoiSnapshot
{
	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("description")] public string Description { get; set; } = "";
	[JsonPropertyName("latitude")] public double Latitude { get; set; }
	[JsonPropertyName("longitude")] public double Longitude { get; set; }
	[JsonPropertyName("mapX")] public double MapX { get; set; }
	[JsonPropertyName("mapY")] public double MapY { get; set; }
	[JsonPropertyName("triggerRadiusMeters")] public double TriggerRadiusMeters { get; set; }
	[JsonPropertyName("cooldownSeconds")] public int CooldownSeconds { get; set; }
	[JsonPropertyName("priority")] public int Priority { get; set; }
	[JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
	[JsonPropertyName("audioViUrl")] public string? AudioViUrl { get; set; }
	[JsonPropertyName("category")] public int Category { get; set; }
	[JsonPropertyName("qrCode")] public string? QrCode { get; set; }
	[JsonPropertyName("contentVersion")] public int ContentVersion { get; set; }
	[JsonPropertyName("translations")] public List<PoiTranslationSnapshot>? Translations { get; set; }

	public string ResolveName(string lang)
	{
		var t = Translations?.FirstOrDefault(x => x.LanguageCode == lang);
		return string.IsNullOrWhiteSpace(t?.Name) ? Name : t.Name;
	}

	public string ResolveDescription(string lang)
	{
		var t = Translations?.FirstOrDefault(x => x.LanguageCode == lang);
		if (!string.IsNullOrWhiteSpace(t?.Description)) return t.Description;
		t = Translations?.FirstOrDefault(x => x.LanguageCode == "en");
		if (!string.IsNullOrWhiteSpace(t?.Description)) return t.Description;
		return Description;
	}

	public string? ResolveAudioUrl(string lang)
	{
		var t = Translations?.FirstOrDefault(x => x.LanguageCode == lang);
		var audioUrl = !string.IsNullOrWhiteSpace(t?.AudioUrl) ? t.AudioUrl
			: (lang == "vi" && !string.IsNullOrWhiteSpace(AudioViUrl) ? AudioViUrl
			: Translations?.FirstOrDefault(x => x.LanguageCode == "en")?.AudioUrl);

		// Debug logging
		System.Diagnostics.Debug.WriteLine($"[ResolveAudioUrl] Lang={lang}, FoundUrl={audioUrl ?? "(null)"}, " +
			$"TranslationsCount={Translations?.Count ?? 0}, " +
			$"HasEnAudio={Translations?.Any(x => x.LanguageCode == "en" && !string.IsNullOrEmpty(x.AudioUrl)) ?? false}");

		return audioUrl;
	}
}

public sealed class PoiTranslationSnapshot
{
	[JsonPropertyName("id")] public int Id { get; set; }
	[JsonPropertyName("poiId")] public int PoiId { get; set; }
	[JsonPropertyName("languageCode")] public string LanguageCode { get; set; } = "vi";
	[JsonPropertyName("name")] public string Name { get; set; } = "";
	[JsonPropertyName("description")] public string Description { get; set; } = "";
	[JsonPropertyName("audioUrl")] public string? AudioUrl { get; set; }
	[JsonPropertyName("originalDescription")] public string OriginalDescription { get; set; } = "";
}
