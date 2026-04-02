using System.Text.Json;
using SQLite;
using VinhKhanh.App.Data;
using VinhKhanh.App.Models;

namespace VinhKhanh.App.Services;

public sealed class LocalPoiCacheService
{
	private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
	private SQLiteAsyncConnection? _db;

	private async Task<SQLiteAsyncConnection> GetDbAsync()
	{
		if (_db != null) return _db;
		var path = Path.Combine(FileSystem.AppDataDirectory, "vinhkhanh_local.db");
		_db = new SQLiteAsyncConnection(path);
		await _db.CreateTableAsync<CachedPoiEntity>();
		return _db;
	}

	public async Task SavePoisAsync(IEnumerable<PoiSnapshot> pois, CancellationToken ct = default)
	{
		var db = await GetDbAsync();
		foreach (var p in pois)
		{
			ct.ThrowIfCancellationRequested();
			var json = JsonSerializer.Serialize(p, JsonOpts);
			await db.InsertOrReplaceAsync(new CachedPoiEntity
			{
				Id = p.Id,
				PayloadJson = json,
				UpdatedUtcTicks = DateTime.UtcNow.Ticks
			});
		}
	}

	public async Task<IReadOnlyList<PoiSnapshot>> LoadPoisAsync(CancellationToken ct = default)
	{
		var db = await GetDbAsync();
		var rows = await db.Table<CachedPoiEntity>().ToListAsync();
		var list = new List<PoiSnapshot>();
		foreach (var row in rows)
		{
			ct.ThrowIfCancellationRequested();
			var p = JsonSerializer.Deserialize<PoiSnapshot>(row.PayloadJson, JsonOpts);
			if (p != null) list.Add(p);
		}
		return list;
	}
}
