using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanh.App.Services;

namespace VinhKhanh.App.ViewModels;

public partial class SettingsViewModel(
	ILocalDbService db,
	ApiClientService api) : ObservableObject
{
	[ObservableProperty] private string _selectedLanguage = "vi";

	// 0.5 = khắt hơn (7.5m), 1.0 = mặc định (15m), 2.0 = rộng hơn (30m)
	[ObservableProperty] private double _gpsRadiusMultiplier = 1.0;

	[ObservableProperty] private string _selectedTtsVoice = "vi-VN-HoaiMyNeural";

	[ObservableProperty] private bool _isSyncing;
	[ObservableProperty] private string _syncStatus = "";
	[ObservableProperty] private int _cachedPoiCount;

	public List<string> Languages { get; } = ["vi", "en", "zh", "ko", "ja", "km", "th"];

	public List<string> TtsVoices { get; } =
	[
		"vi-VN-HoaiMyNeural",
		"en-US-AriaNeural",
		"zh-CN-XiaoxiaoNeural",
		"ko-KR-SunHiNeural",
		"ja-JP-NanamiNeural",
		"km-KH-SreymomNeural",
		"th-TH-PremwadeeNeural"
	];

	protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		switch (e.PropertyName)
		{
			case nameof(SelectedLanguage):
				Microsoft.Maui.Storage.Preferences.Set(AppPreferences.UiLanguage, SelectedLanguage);
				break;
			case nameof(GpsRadiusMultiplier):
				Microsoft.Maui.Storage.Preferences.Set("gps_radius_multiplier", (float)GpsRadiusMultiplier);
				break;
			case nameof(SelectedTtsVoice):
				Microsoft.Maui.Storage.Preferences.Set("tts_voice", SelectedTtsVoice);
				break;
		}
	}

	[RelayCommand]
	private async Task LoadAsync()
	{
		SelectedLanguage = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
		GpsRadiusMultiplier = Microsoft.Maui.Storage.Preferences.Get("gps_radius_multiplier", 1.0f);
		SelectedTtsVoice = Microsoft.Maui.Storage.Preferences.Get("tts_voice", "vi-VN-HoaiMyNeural");
		CachedPoiCount = await db.CountPoisAsync();
	}

	[RelayCommand]
	private async Task SyncOfflineDataAsync()
	{
		IsSyncing = true;
		SyncStatus = "Đang tải POI từ server...";
		try
		{
			var poiSnapshots = await api.GetPoisAsync(SelectedLanguage);
			var pois = poiSnapshots.Select(p => new VinhKhanh.Infrastructure.Data.Poi
			{
				Id = p.Id,
				Name = p.Name,
				Description = p.Description,
				Latitude = p.Latitude,
				Longitude = p.Longitude,
				MapX = p.MapX,
				MapY = p.MapY,
				TriggerRadiusMeters = p.TriggerRadiusMeters,
				CooldownSeconds = p.CooldownSeconds,
				Priority = p.Priority,
				ImageUrl = p.ImageUrl,
				AudioViUrl = p.AudioViUrl
			}).ToList();
			await db.SavePoisAsync(pois);
			CachedPoiCount = pois.Count;
			SyncStatus = $"Đã tải {pois.Count} POI thành công.";
		}
		catch (Exception ex)
		{
			SyncStatus = $"Lỗi khi tải: {ex.Message}";
		}
		finally { IsSyncing = false; }
	}
}
