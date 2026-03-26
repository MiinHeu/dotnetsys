using SQLite;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.Services;

public interface ILocalDbService
{
	Task<List<Poi>> GetPoisAsync();
	Task SavePoisAsync(List<Poi> pois);
	Task<int> CountPoisAsync();
	Task<List<Tour>> GetToursAsync();
	Task SaveToursAsync(List<Tour> tours);
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
			Priority = l.Priority, AudioViUrl = l.AudioViUrl, ImageUrl = l.ImageUrl
		}).ToList();
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
			Priority = p.Priority, AudioViUrl = p.AudioViUrl, ImageUrl = p.ImageUrl
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
			Id = t.Id, Name = t.Name, Description = t.Description,
			EstimatedMinutes = t.EstimatedMinutes
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
}

// FIX #1: LocalTour class — được định nghĩa đầy đủ
[SQLite.Table("Tours")]
public class LocalTour
{
	[SQLite.PrimaryKey] public int Id { get; set; }
	public string Name { get; set; } = "";
	public string Description { get; set; } = "";
	public int EstimatedMinutes { get; set; }
}
