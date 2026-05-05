using SQLite;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.App.Models;

namespace VinhKhanh.App.Services;

public interface ILocalDbService
{
	Task<List<Poi>> GetPoisAsync();
	Task SavePoisAsync(List<Poi> pois);
	Task SyncPoisAsync(IEnumerable<PoiSnapshot> snapshots);
	Task<int> CountPoisAsync();
	Task<List<Tour>> GetToursAsync();
	Task SaveToursAsync(List<Tour> tours);
	Task<List<int>> GetVisitedPoiIdsAsync();
	Task AddVisitedPoiAsync(int poiId);
	Task<List<TourStop>> GetTourStopsAsync(int tourId);
	Task SaveTourStopsAsync(int tourId, List<TourStop> stops);
}

public class LocalDbService : ILocalDbService
{
	private readonly string _dbPath =
		Path.Combine(FileSystem.AppDataDirectory, "vinh_khanh.db3");
	private SQLiteAsyncConnection? _db;

	private async Task<SQLiteAsyncConnection> GetDbAsync()
	{
		if (_db != null) return _db;
		_db = new SQLiteAsyncConnection(_dbPath);
		await _db.CreateTableAsync<LocalPoi>();
		await _db.CreateTableAsync<LocalTour>();
		await _db.CreateTableAsync<LocalTourStop>();
		await _db.CreateTableAsync<VisitedPoi>();
		return _db;
	}

	public async Task<List<Poi>> GetPoisAsync()
	{
		var conn = await GetDbAsync();
		var local = await conn.Table<LocalPoi>().ToListAsync();
		return local.Select(l => new Poi
		{
			Id = l.Id, Name = l.Name, Description = l.Description,
			Latitude = l.Latitude, Longitude = l.Longitude,
			MapX = l.MapX, MapY = l.MapY,
			TriggerRadiusMeters = l.TriggerRadiusMeters,
			CooldownSeconds = l.CooldownSeconds,
			Priority = l.Priority, AudioViUrl = l.AudioViUrl, ImageUrl = l.ImageUrl,
			Category = Enum.TryParse<PoiCategory>(l.Category, out var c) ? c : PoiCategory.ComTam,
			Address = l.Address,
			PhoneNumber = l.PhoneNumber,
			Rating = l.Rating,
			ImagesJson = l.ImagesJson,
			MenuJson = l.MenuJson,
			TagsJson = l.TagsJson
		}).ToList();
	}

	public async Task SyncPoisAsync(IEnumerable<PoiSnapshot> snapshots)
	{
		var conn = await GetDbAsync();
		await conn.DeleteAllAsync<LocalPoi>();
		await conn.InsertAllAsync(snapshots.Select(p => new LocalPoi
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
			AudioViUrl = p.AudioViUrl,
			ImageUrl = p.ImageUrl,
			Category = p.Category,
			Address = p.Address,
			PhoneNumber = p.PhoneNumber,
			Rating = p.Rating,
			ImagesJson = p.ImagesJson,
			MenuJson = p.MenuJson,
			TagsJson = p.TagsJson
		}));
	}

	public async Task SavePoisAsync(List<Poi> pois)
	{
		var conn = await GetDbAsync();
		await conn.DeleteAllAsync<LocalPoi>();
		await conn.InsertAllAsync(pois.Select(p => new LocalPoi
		{
			Id = p.Id, Name = p.Name, Description = p.Description,
			Latitude = p.Latitude, Longitude = p.Longitude,
			MapX = p.MapX, MapY = p.MapY,
			TriggerRadiusMeters = p.TriggerRadiusMeters,
			CooldownSeconds = p.CooldownSeconds,
			Priority = p.Priority, AudioViUrl = p.AudioViUrl, ImageUrl = p.ImageUrl,
			Category = p.Category.ToString(),
			Address = p.Address,
			PhoneNumber = p.PhoneNumber,
			Rating = p.Rating,
			ImagesJson = p.ImagesJson,
			MenuJson = p.MenuJson,
			TagsJson = p.TagsJson
		}));
	}

	public async Task<int> CountPoisAsync()
		=> await (await GetDbAsync()).Table<LocalPoi>().CountAsync();

	public async Task<List<Tour>> GetToursAsync()
	{
		var conn = await GetDbAsync();
		var local = await conn.Table<LocalTour>().ToListAsync();
		return local.Select(l => new Tour
		{
			Id = l.Id, Name = l.Name, Description = l.Description,
			EstimatedMinutes = l.EstimatedMinutes
		}).ToList();
	}

	public async Task SaveToursAsync(List<Tour> tours)
	{
		var conn = await GetDbAsync();
		await conn.DeleteAllAsync<LocalTour>();
		await conn.InsertAllAsync(tours.Select(t => new LocalTour
		{
			Id = t.Id, Name = t.Name, Description = t.Description ?? string.Empty,
			EstimatedMinutes = t.EstimatedMinutes
		}));
	}

	public async Task<List<int>> GetVisitedPoiIdsAsync()
	{
		var conn = await GetDbAsync();
		var visited = await conn.Table<VisitedPoi>().ToListAsync();
		return visited.Select(v => v.PoiId).ToList();
	}

	public async Task AddVisitedPoiAsync(int poiId)
	{
		var conn = await GetDbAsync();
		var exists = await conn.Table<VisitedPoi>().Where(v => v.PoiId == poiId).CountAsync() > 0;
		if (!exists)
		{
			await conn.InsertAsync(new VisitedPoi { PoiId = poiId, VisitedAt = DateTime.UtcNow });
		}
	}

	public async Task<List<TourStop>> GetTourStopsAsync(int tourId)
	{
		var conn = await GetDbAsync();
		var stops = await conn.Table<LocalTourStop>().Where(s => s.TourId == tourId).ToListAsync();
		var pois = await GetPoisAsync();
		
		return stops.Select(s => new TourStop
		{
			Id = s.Id, TourId = s.TourId, PoiId = s.PoiId, StopOrder = s.StopOrder,
			Poi = pois.FirstOrDefault(p => p.Id == s.PoiId)
		}).ToList();
	}

	public async Task SaveTourStopsAsync(int tourId, List<TourStop> stops)
	{
		var conn = await GetDbAsync();
		await conn.Table<LocalTourStop>().Where(s => s.TourId == tourId).DeleteAsync();
		await conn.InsertAllAsync(stops.Select(s => new LocalTourStop
		{
			Id = s.Id, TourId = s.TourId, PoiId = s.PoiId, StopOrder = s.StopOrder
		}));
	}
}

// SQLite local models — nhẹ hơn domain models
[SQLite.Table("Pois")]
public class LocalPoi
{
	[SQLite.PrimaryKey] public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";
	public double Latitude { get; set; }
	public double Longitude { get; set; }
	public double MapX { get; set; }
	public double MapY { get; set; }
	public double TriggerRadiusMeters { get; set; }
	public int CooldownSeconds { get; set; }
	public int Priority { get; set; }
	public string? AudioViUrl { get; set; }
	public string? ImageUrl { get; set; }
	public string Category { get; set; } = "";
	public string? Address { get; set; }
	public string? PhoneNumber { get; set; }
	public double Rating { get; set; }
	public string? ImagesJson { get; set; }
	public string? MenuJson { get; set; }
	public string? TagsJson { get; set; }
}

[SQLite.Table("Tours")]
public class LocalTour
{
	[SQLite.PrimaryKey] public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";
	public int EstimatedMinutes { get; set; }
}

[SQLite.Table("TourStops")]
public class LocalTourStop
{
	[SQLite.PrimaryKey] public int Id { get; set; }
	[SQLite.Indexed] public int TourId { get; set; }
	public int PoiId { get; set; }
	public int StopOrder { get; set; }
}

[SQLite.Table("VisitedPois")]
public class VisitedPoi
{
	[SQLite.PrimaryKey] public int PoiId { get; set; }
	public DateTime VisitedAt { get; set; }
}
