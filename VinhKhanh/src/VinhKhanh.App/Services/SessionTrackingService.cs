using System.Diagnostics;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.Messaging;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.App.Services;

/// <summary>
/// Thu thập thông tin thiết bị và quản lý vòng đời phiên du khách.
/// - Khi mở App: gửi POST /api/session/start (device info)
/// - Mỗi 60 giây: gửi POST /api/session/heartbeat (POIs visited, distance)
/// - Khi đóng App: gửi POST /api/session/end
/// </summary>
public sealed class SessionTrackingService : IDisposable
{
	private readonly ApiClientService _api;
	private readonly SessionService _session;
	private readonly Timer _heartbeatTimer;

	private int _poisVisited;
	private double _totalDistanceMeters;
	private double _lastLat;
	private double _lastLon;
	private bool _hasLastPosition;
	private bool _sessionStarted;

	public SessionTrackingService(ApiClientService api, SessionService session)
	{
		_api = api;
		_session = session;

		// Timer gửi heartbeat mỗi 60 giây
		_heartbeatTimer = new Timer(OnHeartbeatTick, null, Timeout.Infinite, Timeout.Infinite);

		// Đăng ký lắng nghe GPS để tính quãng đường
		WeakReferenceMessenger.Default.Register<LocationUpdatedMessage>(this, (_, msg) =>
		{
			UpdateDistance(msg.Location.Latitude, msg.Location.Longitude);
		});
	}

	/// <summary>Gọi khi App được mở hoặc resume — gửi thông tin thiết bị lên server.</summary>
	public async Task StartSessionAsync()
	{
		if (_sessionStarted) return;
		_sessionStarted = true;

		var dto = new SessionStartDto(
			SessionId: _session.SessionId,
			DeviceModel: Microsoft.Maui.Devices.DeviceInfo.Model ?? "Unknown",
			DevicePlatform: Microsoft.Maui.Devices.DeviceInfo.Platform.ToString(),
			OsVersion: Microsoft.Maui.Devices.DeviceInfo.VersionString ?? "",
			AppVersion: Microsoft.Maui.ApplicationModel.AppInfo.VersionString ?? "1.0.0",
			Manufacturer: Microsoft.Maui.Devices.DeviceInfo.Manufacturer ?? "Unknown",
			LanguageUsed: Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi")
		);

		try
		{
			using var http = _api.CreateClient();
			var res = await http.PostAsJsonAsync("api/session/start", dto);
			if (res.IsSuccessStatusCode)
			{
				Debug.WriteLine($"[SessionTracking] Session started: {dto.DeviceModel} ({dto.DevicePlatform} {dto.OsVersion})");
			}
			else
			{
				Debug.WriteLine($"[SessionTracking] Start failed: {res.StatusCode}");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SessionTracking] Start error: {ex.Message}");
		}

		// Bắt đầu heartbeat timer: lần đầu sau 60s, sau đó mỗi 60s
		_heartbeatTimer.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
	}

	/// <summary>Gọi khi App đóng hoặc chuyển sang background — đánh dấu kết thúc phiên.</summary>
	public async Task EndSessionAsync()
	{
		if (!_sessionStarted) return;
		_sessionStarted = false;

		// Dừng heartbeat
		_heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);

		var dto = new SessionEndDto(
			SessionId: _session.SessionId,
			PoisVisited: _poisVisited,
			DistanceMeters: Math.Round(_totalDistanceMeters, 1)
		);

		try
		{
			using var http = _api.CreateClient();
			var res = await http.PostAsJsonAsync("api/session/end", dto);
			Debug.WriteLine($"[SessionTracking] Session ended: POIs={_poisVisited}, Distance={_totalDistanceMeters:F0}m, Status={res.StatusCode}");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SessionTracking] End error: {ex.Message}");
		}
	}

	/// <summary>Gọi từ MainViewModel khi du khách nghe thuyết minh 1 quán.</summary>
	public void IncrementPoisVisited()
	{
		Interlocked.Increment(ref _poisVisited);
	}

	/// <summary>Tính khoảng cách giữa 2 tọa độ GPS liên tiếp.</summary>
	private void UpdateDistance(double lat, double lon)
	{
		if (!_hasLastPosition)
		{
			_lastLat = lat;
			_lastLon = lon;
			_hasLastPosition = true;
			return;
		}

		var distance = VinhKhanh.Shared.GeoMath.Haversine(_lastLat, _lastLon, lat, lon);

		// Bỏ qua GPS jitter < 2m và nhảy bất thường > 500m
		if (distance >= 2 && distance <= 500)
		{
			_totalDistanceMeters += distance;
		}

		_lastLat = lat;
		_lastLon = lon;
	}

	private async void OnHeartbeatTick(object? state)
	{
		if (!_sessionStarted) return;

		var dto = new SessionHeartbeatDto(
			SessionId: _session.SessionId,
			PoisVisited: _poisVisited,
			DistanceMeters: Math.Round(_totalDistanceMeters, 1)
		);

		try
		{
			using var http = _api.CreateClient();
			await http.PostAsJsonAsync("api/session/heartbeat", dto);
			Debug.WriteLine($"[SessionTracking] Heartbeat: POIs={_poisVisited}, Distance={_totalDistanceMeters:F0}m");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[SessionTracking] Heartbeat error: {ex.Message}");
		}
	}

	public void Dispose()
	{
		WeakReferenceMessenger.Default.Unregister<LocationUpdatedMessage>(this);
		_heartbeatTimer.Dispose();
	}
}
