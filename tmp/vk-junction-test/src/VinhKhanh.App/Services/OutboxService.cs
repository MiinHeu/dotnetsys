using System.Text.Json;
using SQLite;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.App.Services;

public interface IOutboxService
{
	Task EnqueueMovementBatchAsync(MovementBatchDto dto);
	Task EnqueueHistoryAsync(AppHistoryLogDto dto);
	Task EnqueueVisitAsync(VisitLogDto dto);
	Task<int> FlushAsync(ApiClientService api, CancellationToken ct = default);
}

public sealed class OutboxService : IOutboxService
{
	private readonly string _dbPath = Path.Combine(FileSystem.AppDataDirectory, "vinh_khanh.db3");
	private SQLiteAsyncConnection? _db;
	private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

	private async Task<SQLiteAsyncConnection> GetDbAsync()
	{
		if (_db != null) return _db;
		_db = new SQLiteAsyncConnection(_dbPath);
		await _db.CreateTableAsync<OutboxItem>();
		return _db;
	}

	public Task EnqueueMovementBatchAsync(MovementBatchDto dto)
		=> EnqueueAsync("movement", JsonSerializer.Serialize(dto, JsonOpts));

	public Task EnqueueHistoryAsync(AppHistoryLogDto dto)
		=> EnqueueAsync("history", JsonSerializer.Serialize(dto, JsonOpts));

	public Task EnqueueVisitAsync(VisitLogDto dto)
		=> EnqueueAsync("visit", JsonSerializer.Serialize(dto, JsonOpts));

	public async Task<int> FlushAsync(ApiClientService api, CancellationToken ct = default)
	{
		var db = await GetDbAsync();
		var items = await db.Table<OutboxItem>().OrderBy(x => x.CreatedAtUtcTicks).Take(100).ToListAsync();
		var sent = 0;

		foreach (var item in items)
		{
			ct.ThrowIfCancellationRequested();
			var ok = false;
			try
			{
				switch (item.Kind)
				{
					case "movement":
						var mb = JsonSerializer.Deserialize<MovementBatchDto>(item.PayloadJson, JsonOpts);
						if (mb != null) ok = await api.TryPostMovementBatchAsync(mb, ct);
						break;
					case "history":
						var h = JsonSerializer.Deserialize<AppHistoryLogDto>(item.PayloadJson, JsonOpts);
						if (h != null) ok = await api.TryPostHistoryLogAsync(h, ct);
						break;
					case "visit":
						var v = JsonSerializer.Deserialize<VisitLogDto>(item.PayloadJson, JsonOpts);
						if (v != null) ok = await api.TryPostAnalyticsVisitAsync(v, ct);
						break;
				}
			}
			catch
			{
				ok = false;
			}

			if (ok)
			{
				await db.DeleteAsync(item);
				sent++;
			}
			else
			{
				item.RetryCount++;
				await db.UpdateAsync(item);
				break;
			}
		}

		return sent;
	}

	private async Task EnqueueAsync(string kind, string payload)
	{
		var db = await GetDbAsync();
		await db.InsertAsync(new OutboxItem
		{
			Kind = kind,
			PayloadJson = payload,
			CreatedAtUtcTicks = DateTime.UtcNow.Ticks
		});
	}

	[Table("outbox_items")]
	private sealed class OutboxItem
	{
		[PrimaryKey, AutoIncrement] public int Id { get; set; }
		public string Kind { get; set; } = "";
		public string PayloadJson { get; set; } = "";
		public long CreatedAtUtcTicks { get; set; }
		public int RetryCount { get; set; }
	}
}
