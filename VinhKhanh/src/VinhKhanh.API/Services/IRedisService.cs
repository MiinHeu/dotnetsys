namespace VinhKhanh.API.Services;

public interface IRedisService
{
	Task SetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default);
	Task<string?> GetAsync(string key, CancellationToken ct = default);
	Task DeleteAsync(string key, CancellationToken ct = default);
	Task PublishAsync(string channel, string message, CancellationToken ct = default);
}
