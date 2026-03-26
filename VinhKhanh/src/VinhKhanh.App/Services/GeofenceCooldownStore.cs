using System.Text.Json;

namespace VinhKhanh.App.Services;

public sealed class GeofenceCooldownStore
{
	private readonly Dictionary<int, DateTime> _lastFire = new();

	public GeofenceCooldownStore()
	{
		Load();
	}

	private void Load()
	{
		var json = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.CooldownState, null);
		if (string.IsNullOrWhiteSpace(json)) return;
		try
		{
			var dict = JsonSerializer.Deserialize<Dictionary<int, long>>(json);
			if (dict == null) return;
			foreach (var kv in dict)
				_lastFire[kv.Key] = new DateTime(kv.Value, DateTimeKind.Utc);
		}
		catch { /* ignore */ }
	}

	private void Save()
	{
		var dict = _lastFire.ToDictionary(kv => kv.Key, kv => kv.Value.Ticks);
		Microsoft.Maui.Storage.Preferences.Set(AppPreferences.CooldownState, JsonSerializer.Serialize(dict));
	}

	public bool CanTrigger(int poiId, int cooldownSeconds)
	{
		if (!_lastFire.TryGetValue(poiId, out var last))
			return true;
		return (DateTime.UtcNow - last).TotalSeconds >= cooldownSeconds;
	}

	public void MarkTriggered(int poiId)
	{
		_lastFire[poiId] = DateTime.UtcNow;
		Save();
	}
}
