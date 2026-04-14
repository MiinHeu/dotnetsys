using System.Text.Json;
using SQLite;
using VinhKhanh.App.Data;
using VinhKhanh.App.Models;

namespace VinhKhanh.App.Services;

public sealed class LocalPoiCacheService
{
	private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
	private static readonly JsonSerializerOptions TourJsonOpts = new() { PropertyNameCaseInsensitive = true };
	private SQLiteAsyncConnection? _db;

	private async Task<SQLiteAsyncConnection> GetDbAsync()
	{
		if (_db != null) return _db;
		var path = Path.Combine(FileSystem.AppDataDirectory, "vinhkhanh_local.db");
		_db = new SQLiteAsyncConnection(path);
		await _db.CreateTableAsync<CachedPoiEntity>();
		await _db.CreateTableAsync<CachedTourEntity>();
		return _db;
	}

	public async Task SavePoisAsync(IEnumerable<PoiSnapshot> pois, CancellationToken ct = default)
	{
		var db = await GetDbAsync();

		// Clear existing and replace to handle deactivations/deletions correctly
		var existing = await db.Table<CachedPoiEntity>().ToListAsync();
		foreach (var e in existing) await db.DeleteAsync(e);

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

	public async Task SaveToursAsync(IEnumerable<TourSnapshot> tours, CancellationToken ct = default)
	{
		var db = await GetDbAsync();
		// Clear existing and replace
		var existing = await db.Table<CachedTourEntity>().ToListAsync();
		foreach (var e in existing) await db.DeleteAsync(e);

		foreach (var t in tours)
		{
			ct.ThrowIfCancellationRequested();
			var json = JsonSerializer.Serialize(t, TourJsonOpts);
			await db.InsertAsync(new CachedTourEntity
			{
				Id = t.Id,
				PayloadJson = json,
				UpdatedUtcTicks = DateTime.UtcNow.Ticks
			});
		}
	}

	public async Task<IReadOnlyList<TourSnapshot>> LoadToursAsync(CancellationToken ct = default)
	{
		var db = await GetDbAsync();
		var rows = await db.Table<CachedTourEntity>().ToListAsync();
		var list = new List<TourSnapshot>();
		foreach (var row in rows)
		{
			ct.ThrowIfCancellationRequested();
			var t = JsonSerializer.Deserialize<TourSnapshot>(row.PayloadJson, TourJsonOpts);
			if (t != null) list.Add(t);
		}
		return list;
	}
}
