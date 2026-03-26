using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanh.App.Models;
using VinhKhanh.App.Services;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.App.ViewModels;

public partial class MainViewModel : ObservableObject, IRecipient<LocationUpdatedMessage>
{
	private readonly ApiClientService _api;
	private readonly LocalPoiCacheService _cache;
	private readonly SessionService _session;
	private readonly GeofenceCooldownStore _cooldowns;
	private readonly INarrationService _narration;
	private readonly IGpsService _gps;
	private readonly IGeofenceService _geofence;

	private readonly List<MovementPointDto> _movementBuffer = [];
	private DateTime _lastMovementFlush = DateTime.UtcNow;

	public MainViewModel(
		ApiClientService api,
		LocalPoiCacheService cache,
		SessionService session,
		GeofenceCooldownStore cooldowns,
		INarrationService narration,
		IGpsService gps,
		IGeofenceService geofence)
	{
		_api = api;
		_cache = cache;
		_session = session;
		_cooldowns = cooldowns;
		_narration = narration;
		_gps = gps;
		_geofence = geofence;
		SelectedLanguage = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
		WeakReferenceMessenger.Default.Register(this);
	}

	public ObservableCollection<PoiSnapshot> Pois { get; } = new();

	[ObservableProperty] private string _selectedLanguage = "vi";
	[ObservableProperty] private string _statusMessage = "";
	[ObservableProperty] private string _nearestLabel = "";
	[ObservableProperty] private bool _isTracking;
	[ObservableProperty] private double _userLatitude = 10.7535;
	[ObservableProperty] private double _userLongitude = 106.6783;

	partial void OnSelectedLanguageChanged(string value)
		=> Microsoft.Maui.Storage.Preferences.Set(AppPreferences.UiLanguage, value);

	[RelayCommand]
	private async Task SyncPoisAsync()
	{
		StatusMessage = "Dang dong bo POI...";
		try
		{
			var remote = await _api.GetPoisAsync(SelectedLanguage);
			if (remote.Count > 0)
			{
				await _cache.SavePoisAsync(remote);
				ReplacePois(remote);
				StatusMessage = $"Da dong bo {remote.Count} POI.";
			}
			else
			{
				var local = await _cache.LoadPoisAsync();
				ReplacePois(local);
				StatusMessage = local.Count > 0
					? $"Offline: {local.Count} POI trong cache."
					: "Khong co du lieu. Kiem tra API.";
			}

			await _api.PostHistoryLogAsync(new AppHistoryLogDto(_session.SessionId, "SYNC_POI",
				LanguageCode: SelectedLanguage, Payload: $"count={Pois.Count}"));
		}
		catch (Exception ex)
		{
			StatusMessage = "Loi mang — dang doc cache.";
			Debug.WriteLine(ex);
			var local = await _cache.LoadPoisAsync();
			ReplacePois(local);
		}
	}

	private void ReplacePois(IReadOnlyList<PoiSnapshot> list)
	{
		Pois.Clear();
		foreach (var p in list.OrderByDescending(x => x.Priority))
			Pois.Add(p);
	}

	[RelayCommand]
	private async Task ToggleTrackingAsync()
	{
		if (IsTracking)
		{
			await _gps.StopTrackingAsync();
			IsTracking = false;
			StatusMessage = "Da tat theo doi.";
			await FlushMovementAsync();
			return;
		}

		IsTracking = true;
		StatusMessage = "Dang theo doi GPS qua Dịch vụ nền...";
		await _gps.StartTrackingAsync();
	}

	public async void Receive(LocationUpdatedMessage message)
	{
		var loc = message.Location;
		UserLatitude = loc.Latitude;
		UserLongitude = loc.Longitude;

		_movementBuffer.Add(new MovementPointDto(loc.Latitude, loc.Longitude,
			(float)(loc.Accuracy ?? 25), DateTime.UtcNow));
		await MaybeFlushMovementAsync();

		// Chuyển PoisSnapshot -> Poi domain model
		var domainPois = Pois.Select(p => new Poi
		{
			Id = p.Id,
			Name = p.Name,
			Description = p.Description,
			Latitude = p.Latitude,
			Longitude = p.Longitude,
			TriggerRadiusMeters = p.TriggerRadiusMeters,
			Priority = p.Priority,
			CooldownSeconds = p.CooldownSeconds
		}).ToList();

		var triggered = await _geofence.CheckTriggeredAsync(loc, domainPois);
		var best = triggered.FirstOrDefault();
		
		if (best != null && !_narration.IsPlaying)
		{
			NearestLabel = best.Name;
			StatusMessage = $"Thuyet minh: {NearestLabel}";
			
			// Dùng INarrationService để phát audio
			await _narration.EnqueueAsync(best, SelectedLanguage);

			await _api.PostAnalyticsVisitAsync(new VisitLogDto(best.Id, _session.SessionId, SelectedLanguage, "GPS", 1));
			await _api.PostHistoryLogAsync(new AppHistoryLogDto(_session.SessionId, "GPS_TRIGGER", PoiId: best.Id, LanguageCode: SelectedLanguage));
		}
		else if (best != null)
		{
			NearestLabel = best.Name;
		}
	}

	private async Task MaybeFlushMovementAsync()
	{
		if (_movementBuffer.Count >= 25 || (DateTime.UtcNow - _lastMovementFlush).TotalSeconds > 30)
			await FlushMovementAsync();
	}

	private async Task FlushMovementAsync()
	{
		if (_movementBuffer.Count == 0) return;
		var batch = new MovementBatchDto(_session.SessionId, _movementBuffer.ToList());
		_movementBuffer.Clear();
		_lastMovementFlush = DateTime.UtcNow;
		await _api.PostMovementBatchAsync(batch);
	}

	public string ApiRootForAudio =>
		Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase()).TrimEnd('/');
}
