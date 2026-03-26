namespace VinhKhanh.API.Services;

/// <summary>Khi không cấu hình Redis hoặc kết nối thất bại — không chặn khởi động API.</summary>
public sealed class NoOpRedisService : IRedisService
{
	public Task SetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
		=> Task.CompletedTask;

	public Task<string?> GetAsync(string key, CancellationToken ct = default)
		=> Task.FromResult<string?>(null);

	public Task DeleteAsync(string key, CancellationToken ct = default)
		=> Task.CompletedTask;

	public Task PublishAsync(string channel, string message, CancellationToken ct = default)
		=> Task.CompletedTask;
}
