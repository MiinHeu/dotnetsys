using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using VinhKhanh.App.Services;

namespace VinhKhanh.App.Models;

public partial class PoiSnapshot : ObservableObject
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
	private string? _imageUrl;
	[JsonPropertyName("imageUrl")] 
	public string? ImageUrl 
	{ 
		get => FixUrl(_imageUrl); 
		set => _imageUrl = value; 
	}

	[JsonPropertyName("audioViUrl")] public string? AudioViUrl { get; set; }
	[JsonPropertyName("category")] public string Category { get; set; } = "";
	[JsonPropertyName("qrCode")] public string? QrCode { get; set; }
	[JsonPropertyName("contentVersion")] public int ContentVersion { get; set; }
	[JsonPropertyName("translations")] public List<PoiTranslationSnapshot>? Translations { get; set; }
	
	// Thuộc tính hiển thị thông minh (Tự động lấy ngôn ngữ hiện tại)
	[JsonIgnore]
	public string DisplayName => ResolveName(Microsoft.Maui.Storage.Preferences.Get(Services.AppPreferences.UiLanguage, "vi"));

	[JsonIgnore]
	public string DisplayDescription => ResolveDescription(Microsoft.Maui.Storage.Preferences.Get(Services.AppPreferences.UiLanguage, "vi"));

	public void RefreshTranslations()
	{
		OnPropertyChanged(nameof(DisplayName));
		OnPropertyChanged(nameof(DisplayDescription));
		OnPropertyChanged(nameof(ImageUrl));
	}

	public string ResolveName(string lang)
	{
		var t = Translations?.FirstOrDefault(x => x.LanguageCode == lang);
		if (!string.IsNullOrWhiteSpace(t?.Name)) return t.Name;

		// Nếu yêu cầu tiếng Việt, ưu tiên lấy Name gốc thay vì dự phòng sang tiếng Anh
		if (lang == "vi") return Name;

		t = Translations?.FirstOrDefault(x => x.LanguageCode == "en");
		if (!string.IsNullOrWhiteSpace(t?.Name)) return t.Name;

		return Name;
	}

	public string ResolveDescription(string lang)
	{
		var t = Translations?.FirstOrDefault(x => x.LanguageCode == lang);
		if (!string.IsNullOrWhiteSpace(t?.Description)) return t.Description;

		// Nếu yêu cầu tiếng Việt, ưu tiên lấy Description gốc thay vì dự phòng sang tiếng Anh
		if (lang == "vi") return Description;

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

		return FixUrl(audioUrl);
	}

	private static string? FixUrl(string? url)
	{
		if (string.IsNullOrWhiteSpace(url)) return url;

		// Trên giả lập Android, localhost trỏ vào chính nó.
		// Cần đổi sang 10.0.2.2 để truy cập vào máy tính (Host).
		if (Microsoft.Maui.Devices.DeviceInfo.DeviceType == Microsoft.Maui.Devices.DeviceType.Virtual 
		    && Microsoft.Maui.Devices.DeviceInfo.Platform == Microsoft.Maui.Devices.DevicePlatform.Android)
		{
			return url.Replace("localhost", "10.0.2.2")
			          .Replace("127.0.0.1", "10.0.2.2");
		}
		return url;
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
