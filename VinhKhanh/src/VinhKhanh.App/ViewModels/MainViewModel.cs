using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanh.App.Models;
using VinhKhanh.App.Services;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
	private readonly ApiClientService _api;
	private readonly LocalPoiCacheService _cache;
	private readonly SessionService _session;
	private readonly GeofenceCooldownStore _cooldowns;
	private readonly NarrationService _narration;

	private CancellationTokenSource? _trackCts;
	private int _debounceCandidateId;
	private int _debounceHits;
	private readonly List<MovementPointDto> _movementBuffer = [];
	private DateTime _lastMovementFlush = DateTime.UtcNow;

	public MainViewModel(
		ApiClientService api,
		LocalPoiCacheService cache,
		SessionService session,
		GeofenceCooldownStore cooldowns,
		NarrationService narration)
	{
		_api = api;
		_cache = cache;
		_session = session;
		_cooldowns = cooldowns;
		_narration = narration;
		SelectedLanguage = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
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
			_trackCts?.Cancel();
			_trackCts?.Dispose();
			_trackCts = null;
			IsTracking = false;
			StatusMessage = "Da tat theo doi.";
			await FlushMovementAsync();
			return;
		}

		var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
		if (status != PermissionStatus.Granted)
		{
			StatusMessage = "Can quyen vi tri.";
			return;
		}

		_ = await Permissions.RequestAsync<Permissions.LocationAlways>();

		IsTracking = true;
		StatusMessage = "Dang theo doi GPS...";
		_trackCts = new CancellationTokenSource();
		_ = RunTrackingLoopAsync(_trackCts.Token);
	}

	private async Task RunTrackingLoopAsync(CancellationToken ct)
	{
		var mult = 1.0;
		var mstr = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.GpsRadiusMultiplier, "1");
		_ = double.TryParse(mstr, out mult);
		if (mult <= 0) mult = 1;

		var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));

		while (!ct.IsCancellationRequested)
		{
			try
			{
				var loc = await Geolocation.Default.GetLocationAsync(request, ct);
				if (loc != null)
				{
					UserLatitude = loc.Latitude;
					UserLongitude = loc.Longitude;
					_movementBuffer.Add(new MovementPointDto(loc.Latitude, loc.Longitude,
						(float)(loc.Accuracy ?? 25), DateTime.UtcNow));
					await MaybeFlushMovementAsync();

					var best = GeofenceEvaluator.PickBestInside(loc.Latitude, loc.Longitude, Pois.ToList(), mult);
					await ProcessGeofenceAsync(best, ct);
				}
			}
			catch (OperationCanceledException) { break; }
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				StatusMessage = "GPS tam loi — thu lai...";
			}

			try { await Task.Delay(4500, ct); }
			catch (OperationCanceledException) { break; }
		}
	}

	private async Task ProcessGeofenceAsync(PoiSnapshot? inside, CancellationToken ct)
	{
		if (inside == null)
		{
			_debounceHits = 0;
			_debounceCandidateId = 0;
			NearestLabel = "";
			return;
		}

		NearestLabel = inside.ResolveName(SelectedLanguage);

		if (inside.Id != _debounceCandidateId)
		{
			_debounceCandidateId = inside.Id;
			_debounceHits = 1;
			return;
		}

		_debounceHits++;
		if (_debounceHits < 2) return;
		if (!_cooldowns.CanTrigger(inside.Id, inside.CooldownSeconds)) return;

		_cooldowns.MarkTriggered(inside.Id);
		StatusMessage = $"Thuyet minh: {NearestLabel}";

		var apiRoot = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase()).TrimEnd('/');

		var heard = await _narration.PlayPoiAsync(inside, SelectedLanguage, apiRoot, ct);

		await _api.PostAnalyticsVisitAsync(new VisitLogDto(inside.Id, _session.SessionId, SelectedLanguage, "GPS", heard));
		await _api.PostHistoryLogAsync(new AppHistoryLogDto(_session.SessionId, "GPS_TRIGGER",
			PoiId: inside.Id, LanguageCode: SelectedLanguage));

		_debounceHits = 0;
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
