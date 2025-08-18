using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace AccessRefresh.Services.Domain.CacheService;

public class RedisCacheProvider(IConnectionMultiplexer? redis) : ICacheProvider
{
    private readonly IDatabase _db = redis?.GetDatabase()!;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public bool IsConnected => redis?.IsConnected == true;

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!, _jsonOptions) : null;
    }
    
    public async Task<string?> GetStringAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public T? Get<T>(string key) where T : class
    {
        var value = _db.StringGet(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!, _jsonOptions) : null;
    }

    public Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        return _db.StringSetAsync(key, value, expiry);
    }
    
    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var serialized = JsonSerializer.Serialize(value, _jsonOptions);
        return _db.StringSetAsync(key, serialized, expiry);
    }
    
    public Task<bool> RemoveAsync(string key)
    {
        return _db.KeyDeleteAsync(key);
    }
    
    public Task<bool> ExistsAsync(string key)
    {
        return _db.KeyExistsAsync(key);
    }

    public override string ToString() => "Redis";
}