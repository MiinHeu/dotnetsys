using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanh.App.Services;
using VinhKhanh.App.Models;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.ViewModels;

[QueryProperty(nameof(Poi), "Poi")]
public partial class PoiDetailViewModel(NarrationService narration, ApiClientService api) : ObservableObject
{
	[ObservableProperty] private Poi? _poi;
	[ObservableProperty] private bool _isPlaying;
	[ObservableProperty] private string _selectedLanguage = "vi";

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
