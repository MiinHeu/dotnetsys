using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.Services;

/// <summary>
/// Tu dong dong bo POI + Tour ve SQLite khi ket noi Internet tro lai.
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
		if (e.NetworkAccess != NetworkAccess.Internet)
			return;

		var lang = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");

		try
		{
			var poiSnapshots = await _api.GetPoisAsync(lang);
			if (poiSnapshots.Count > 0)
			{
				var pois = poiSnapshots.Select(p => new Poi
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
					IsActive = true
				}).ToList();

				await _db.SavePoisAsync(pois);
			}
		}
		catch
		{
			// Thu lai lan ket noi tiep theo.
		}

		try
		{
			var tours = await _api.GetToursAsync(lang);
			if (tours.Count > 0)
			{
				var localTours = tours.Select(t => new Tour
				{
					Id = t.Id,
					Name = t.Name,
					Description = t.Description,
					EstimatedMinutes = t.EstimatedMinutes,
					IsActive = true
				}).ToList();

				await _db.SaveToursAsync(localTours);
			}
		}
		catch
		{
			// Thu lai lan ket noi tiep theo.
		}
	}
}
