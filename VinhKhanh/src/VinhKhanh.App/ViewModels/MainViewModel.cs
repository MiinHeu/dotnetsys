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
	private readonly IOutboxService _outbox;
	private readonly SessionTrackingService _sessionTracking;

	private readonly ILocalDbService _db;

	private readonly List<MovementPointDto> _movementBuffer = [];
	private DateTime _lastMovementFlush = DateTime.UtcNow;
	private DateTime _lastOutboxFlush = DateTime.MinValue;

	public MainViewModel(
		ApiClientService api,
		LocalPoiCacheService cache,
		SessionService session,
		GeofenceCooldownStore cooldowns,
		INarrationService narration,
		IGpsService gps,
		IGeofenceService geofence,
		IOutboxService outbox,
		SessionTrackingService sessionTracking,
		ILocalDbService db)
	{
		_api = api;
		_cache = cache;
		_session = session;
		_cooldowns = cooldowns;
		_narration = narration;
		_gps = gps;
		_geofence = geofence;
		_outbox = outbox;
		_sessionTracking = sessionTracking;
		_db = db;
		SelectedLanguage = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
		WeakReferenceMessenger.Default.Register(this);
	}

	public ObservableCollection<PoiSnapshot> Pois { get; } = new();

	[ObservableProperty] 
	[NotifyPropertyChangedFor(nameof(PageTitle))]
	[NotifyPropertyChangedFor(nameof(SearchPlaceholder))]
	[NotifyPropertyChangedFor(nameof(EmptyListText))]
	[NotifyPropertyChangedFor(nameof(ReloadButtonText))]
	private string _selectedLanguage = "vi";

	public string PageTitle => VinhKhanh.App.Resources.Strings.AppResources.TabPois;
	public string SearchPlaceholder => VinhKhanh.App.Resources.Strings.AppResources.PoiSearchPlaceholder;
	public string EmptyListText => VinhKhanh.App.Resources.Strings.AppResources.PoiEmptyList;
	public string ReloadButtonText => VinhKhanh.App.Resources.Strings.AppResources.PoiReloadButton;
	[ObservableProperty] private string _statusMessage = "";
	[ObservableProperty] private string _nearestLabel = "";
	[ObservableProperty] private bool _isTracking;
	[ObservableProperty] private double _userLatitude = 10.7535;
	[ObservableProperty] private double _userLongitude = 106.6783;
	[ObservableProperty] private int _nearestPoiId;
	[ObservableProperty] private TourSnapshot? _selectedTour;

	[RelayCommand]
	private void ClearTour()
	{
		SelectedTour = null;
	}

	partial void OnSelectedLanguageChanged(string value)
	{
		Microsoft.Maui.Storage.Preferences.Set(AppPreferences.UiLanguage, value);
		
		// Ép tệp tài nguyên hệ thống nạp lại theo ngôn ngữ mới
		VinhKhanh.App.Resources.Strings.AppResources.Culture = new System.Globalization.CultureInfo(value);
		
		// Kích hoạt cập nhật lại các thuộc tính hiển thị (DisplayName, DisplayDescription) cho toàn bộ danh sách
		foreach (var p in Pois)
		{
			p.RefreshTranslations();
		}

		// Thông báo cho Shell cập nhật lại các Tab
		WeakReferenceMessenger.Default.Send(new LanguageChangedMessage(value));

		// Tự động tải audio cho ngôn ngữ vừa chọn (ưu tiên)
		_ = Task.Run(async () => {
			StatusMessage = "Đang ưu tiên tải dữ liệu âm thanh offline...";
			await _narration.PreFetchAllAsync(Pois, value);
			StatusMessage = "Đã hoàn tất tải dữ liệu âm thanh offline.";
		});
	}

	[RelayCommand]
	private async Task SyncPoisAsync()
	{
		StatusMessage = VinhKhanh.App.Resources.Strings.AppResources.SyncStatusSyncing;
		try
		{
			var remote = await _api.GetPoisAsync(SelectedLanguage);
			
			// Luôn lưu và cập nhật danh sách (kể cả khi trống) để đảm bảo đồng bộ IsActive từ Server
			await _cache.SavePoisAsync(remote);
			await _db.SyncPoisAsync(remote); // Đảm bảo đồng bộ sang SQLite cho Passport/Tours
			ReplacePois(remote);
			
			StatusMessage = string.Format(VinhKhanh.App.Resources.Strings.AppResources.SyncStatusSuccess, remote.Count);

			// Tự động tải audio sau khi Sync metadata xong
			_ = Task.Run(async () => {
				StatusMessage = $"Đang tải audio ({SelectedLanguage})...";
				await _narration.PreFetchAllAsync(remote, SelectedLanguage);
				StatusMessage = $"Đã tải xong toàn bộ dữ liệu offline ({SelectedLanguage}).";
			});

			await _api.PostHistoryLogAsync(new AppHistoryLogDto(_session.SessionId, "SYNC_POI",
				LanguageCode: SelectedLanguage, Payload: $"count={Pois.Count}"));
			
			// Đồng bộ thêm cả Tours để các Tab khác không bị trống
			try 
			{
				var toursVm = MauiProgram.Services.GetRequiredService<ToursViewModel>();
				await toursVm.LoadAsync();
			}
			catch { /* Ignore if tours fail */ }

			await FlushOutboxIfNeededAsync(force: true);
		}
		catch (Exception ex)
		{
			StatusMessage = $"{VinhKhanh.App.Resources.Strings.AppResources.SyncStatusNetworkError}: {ex.Message}";
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
			StatusMessage = VinhKhanh.App.Resources.Strings.AppResources.GpsStatusStopped;
			await FlushMovementAsync();
			await FlushOutboxIfNeededAsync(force: true);
			return;
		}

		IsTracking = true;
		StatusMessage = VinhKhanh.App.Resources.Strings.AppResources.GpsStatusTracking;
		await _gps.StartTrackingAsync();
		await FlushOutboxIfNeededAsync(force: true);
	}

	public async void Receive(LocationUpdatedMessage message)
	{
		var loc = message.Location;
		UserLatitude = loc.Latitude;
		UserLongitude = loc.Longitude;

		_movementBuffer.Add(new MovementPointDto(loc.Latitude, loc.Longitude,
			(float)(loc.Accuracy ?? 25), DateTime.UtcNow));
		await MaybeFlushMovementAsync();
		await FlushOutboxIfNeededAsync();

			var domainPois = Pois.Select(p => new Poi
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
				AudioViUrl = p.AudioViUrl,
				Translations = p.Translations?.Select(t => new PoiTranslation
				{
					Id = t.Id,
					PoiId = t.PoiId,
					LanguageCode = t.LanguageCode,
					Name = t.Name,
					Description = t.Description,
					AudioUrl = t.AudioUrl,
					OriginalDescription = t.OriginalDescription
				}).ToList()
			}).ToList();

		var triggered = await _geofence.CheckTriggeredAsync(loc, domainPois);

		if (triggered.Count > 0)
		{
			// Hiển thị POI có Priority cao nhất trên giao diện
			var best = triggered[0];
			NearestPoiId = best.Id;
			NearestLabel = best.Name;
			StatusMessage = string.Format(VinhKhanh.App.Resources.Strings.AppResources.PlaybackPoiLabel, NearestLabel);

			// Enqueue TẤT CẢ các POI đã trigger — NarrationService sẽ tự sắp xếp theo Priority
			foreach (var poi in triggered)
			{
				if (!_cooldowns.CanTrigger(poi.Id, poi.CooldownSeconds))
					continue;

				await _narration.EnqueueAsync(poi, SelectedLanguage);
				_cooldowns.MarkTriggered(poi.Id);
				_sessionTracking.IncrementPoisVisited();
				await _db.AddVisitedPoiAsync(poi.Id);

				var visit = new VisitLogDto(poi.Id, _session.SessionId, SelectedLanguage, "GPS", 1);
				if (!await _api.TryPostAnalyticsVisitAsync(visit))
					await _outbox.EnqueueVisitAsync(visit);

				var history = new AppHistoryLogDto(_session.SessionId, "GPS_TRIGGER", PoiId: poi.Id, LanguageCode: SelectedLanguage);
				if (!await _api.TryPostHistoryLogAsync(history))
					await _outbox.EnqueueHistoryAsync(history);
			}
		}
		else
		{
			NearestPoiId = 0;
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
		if (!await _api.TryPostMovementBatchAsync(batch))
			await _outbox.EnqueueMovementBatchAsync(batch);
	}

	private async Task FlushOutboxIfNeededAsync(bool force = false)
	{
		if (!force && (DateTime.UtcNow - _lastOutboxFlush).TotalSeconds < 20) return;
		_lastOutboxFlush = DateTime.UtcNow;
		await _outbox.FlushAsync(_api);
	}

	public string ApiRootForAudio =>
		Microsoft.Maui.Storage.Preferences.Get(AppPreferences.ApiBaseUrl, ApiClientService.GetDefaultApiBase()).TrimEnd('/');
}
