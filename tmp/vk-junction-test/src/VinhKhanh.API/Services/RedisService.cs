// using StackExchange.Redis;

namespace VinhKhanh.API.Services;

// Commented out for testing - requires Redis package
/*
public sealed class RedisService(IConnectionMultiplexer redis) : IRedisService
{
	private readonly IDatabase _db = redis.GetDatabase();
	private readonly ISubscriber _sub = redis.GetSubscriber();

	public Task SetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken ct = default)
		=> expiry is { } ex
			? _db.StringSetAsync(key, value, ex)
			: _db.StringSetAsync(key, value);

	public async Task<string?> GetAsync(string key, CancellationToken ct = default)
	{
		var val = await _db.StringGetAsync(key).WaitAsync(ct);
		return val.HasValue ? val.ToString() : null;
	}

	public Task DeleteAsync(string key, CancellationToken ct = default)
		=> _db.KeyDeleteAsync(key);

	public Task PublishAsync(string channel, string message, CancellationToken ct = default)
		=> _sub.PublishAsync(RedisChannel.Literal(channel), message);
}
*/
