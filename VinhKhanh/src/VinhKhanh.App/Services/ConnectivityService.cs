using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.Services;

/// <summary>
/// Lắng nghe sự kiện WiFi kết nối lại → tự đồng bộ POI + Tour vào SQLite
/// </summary>
public class ConnectivityService
{
	private readonly ILocalDbService _db;
	private readonly ApiClientService _api;

	public ConnectivityService(ILocalDbService db, ApiClientService api)
	{
		_db = db;
		_api = api;
		Connectivity.ConnectivityChanged += OnConnectivityChanged;
	}

	private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
	{
		if (e.NetworkAccess != NetworkAccess.Internet) return;
		try
		{
			var lang = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
			var poiSnapshots = await _api.GetPoisAsync(lang);
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
			await _db.SavePoisAsync(pois);
		}
		catch { /* silent fail — sẽ thử lại khi WiFi kết nối lần sau */ }
	}
}
