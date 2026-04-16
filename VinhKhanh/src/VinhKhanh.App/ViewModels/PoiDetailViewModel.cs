using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanh.App.Services;
using VinhKhanh.App.Models;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.ViewModels;

[QueryProperty(nameof(Poi), "Poi")]
public partial class PoiDetailViewModel(NarrationService narration, ApiClientService api) : ObservableObject
{
	[ObservableProperty] 
	[NotifyPropertyChangedFor(nameof(DisplayName))]
	[NotifyPropertyChangedFor(nameof(DisplayDescription))]
	private Poi? _poi;

	[ObservableProperty] private bool _isPlaying;

	[ObservableProperty] 
	[NotifyPropertyChangedFor(nameof(DisplayName))]
	[NotifyPropertyChangedFor(nameof(DisplayDescription))]
	[NotifyPropertyChangedFor(nameof(PageTitle))]
	[NotifyPropertyChangedFor(nameof(PlayNarrationText))]
	[NotifyPropertyChangedFor(nameof(StopNarrationText))]
	[NotifyPropertyChangedFor(nameof(PlaybackLabel))]
	private string _selectedLanguage = "vi";

	public string DisplayName => Poi?.Translations?.FirstOrDefault(t => t.LanguageCode == SelectedLanguage)?.Name ?? Poi?.Name ?? "";
	public string DisplayDescription => Poi?.Translations?.FirstOrDefault(t => t.LanguageCode == SelectedLanguage)?.Description ?? Poi?.Description ?? "";
	
	public string PageTitle => VinhKhanh.App.Resources.Strings.AppResources.TabPois; // Dùng chung key "Quán ăn" hoặc "Restaurants"
	public string PlayNarrationText => VinhKhanh.App.Resources.Strings.AppResources.PlayNarration;
	public string StopNarrationText => VinhKhanh.App.Resources.Strings.AppResources.StopNarration;
	public string PlaybackLabel => VinhKhanh.App.Resources.Strings.AppResources.PlaybackPoiLabel;

	partial void OnPoiChanged(Poi? value)
	{
		SelectedLanguage = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
	}

	[RelayCommand]
	private async Task PlayNarrationAsync()
	{
		if (Poi == null) return;
		IsPlaying = true;
		
		// Convert Poi to PoiSnapshot for NarrationService
		var poiSnapshot = new PoiSnapshot
		{
			Id = Poi.Id,
			Name = Poi.Name,
			Description = Poi.Description,
			Latitude = Poi.Latitude,
			Longitude = Poi.Longitude,
			MapX = Poi.MapX,
			MapY = Poi.MapY,
			TriggerRadiusMeters = Poi.TriggerRadiusMeters,
			CooldownSeconds = Poi.CooldownSeconds,
			Priority = Poi.Priority,
			ImageUrl = Poi.ImageUrl,
			AudioViUrl = Poi.AudioViUrl
		};
		
		await narration.PlayPoiAsync(poiSnapshot, SelectedLanguage, api.ApiRoot);
		IsPlaying = false;
	}

	[RelayCommand]
	private async Task StopNarrationAsync()
	{
		await narration.StopAsync();
		IsPlaying = false;
	}

	[RelayCommand]
	private static Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
