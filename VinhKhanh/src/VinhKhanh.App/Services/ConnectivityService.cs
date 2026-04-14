using VinhKhanh.App.Models;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.Services;

/// <summary>
/// Tu dong dong bo POI + Tour ve SQLite khi ket noi Internet tro lai.
/// Su dung delta sync voi contentVersion de tranh tai du lieu khong can thiet.
/// </summary>
public class ConnectivityService
{
	private readonly LocalPoiCacheService _cache;
	private readonly ApiClientService _api;
	private static readonly string PrefsLastSync = "vk_last_sync_timestamp";
	private static readonly string PrefsKnownVersions = "vk_known_versions";

	public ConnectivityService(LocalPoiCacheService cache, ApiClientService api)
	{
		_cache = cache;
		_api = api;
		Connectivity.ConnectivityChanged += OnConnectivityChanged;
	}

	private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
	{
		// On Emulators, network often reports as Local or ConstrainedInternet
		if (e.NetworkAccess != NetworkAccess.Internet && 
		    e.NetworkAccess != NetworkAccess.Local && 
		    e.NetworkAccess != NetworkAccess.ConstrainedInternet)
			return;

		var lang = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
		await TrySyncPoiAsync(lang);
		await TrySyncToursAsync(lang);
	}

	private async Task TrySyncPoiAsync(string lang)
	{
		try
		{
			var knownVersions = LoadKnownVersions();
			var lastSyncTicks = Microsoft.Maui.Storage.Preferences.Get(PrefsLastSync, 0L);

			var remote = await _api.GetPoisAsync(lang);
			if (remote.Count == 0) return;

			var remoteById = remote.ToDictionary(p => p.Id);
			var newRemoteIds = new HashSet<int>(remoteById.Keys);
			var changed = false;

			// Check for new or updated POIs
			foreach (var p in remote)
			{
				if (!knownVersions.TryGetValue(p.Id, out var cachedVer) || p.ContentVersion > cachedVer)
				{
					changed = true;
					break;
				}
			}

			// Check for deleted/deactivated POIs
			var cached = await _cache.LoadPoisAsync();
			var cachedIds = new HashSet<int>(cached.Select(p => p.Id));
			var removedIds = cachedIds.Except(newRemoteIds).ToList();
			if (removedIds.Count > 0) changed = true;

			if (!changed) return; // nothing changed, skip save

			// Save fresh POIs (replace all on server-authoritative basis)
			await _cache.SavePoisAsync(remote);
			knownVersions = remoteById.ToDictionary(x => x.Key, x => x.Value.ContentVersion >= 1 ? x.Value.ContentVersion : 1);
			Microsoft.Maui.Storage.Preferences.Set(PrefsLastSync, DateTime.UtcNow.Ticks);
			SaveKnownVersions(knownVersions);
		}
		catch { /* retry next reconnect */ }
	}

	private async Task TrySyncToursAsync(string lang)
	{
		try
		{
			var remote = await _api.GetToursAsync(lang);
			if (remote.Count > 0)
			{
				await _cache.SaveToursAsync(remote);
			}
		}
		catch { /* retry next reconnect */ }
	}

	private Dictionary<int, int> LoadKnownVersions()
	{
		var json = Microsoft.Maui.Storage.Preferences.Get("vk_known_versions", "{}");
		try
		{
			return System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new();
		}
		catch { return new(); }
	}

	private void SaveKnownVersions(Dictionary<int, int> versions)
	{
		var json = System.Text.Json.JsonSerializer.Serialize(versions);
		Microsoft.Maui.Storage.Preferences.Set("vk_known_versions", json);
	}
}
