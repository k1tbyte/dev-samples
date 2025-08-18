using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace AccessRefresh.Services.Domain.CacheService;

public class MemoryCacheProvider(IMemoryCache memoryCache) : ICacheProvider
{
    public bool IsConnected => true; // Memory cache always connected
    
    public Task<T?> GetAsync<T>(string key) where T : class
    {
        var result = memoryCache.Get<T>(key);
        return Task.FromResult(result);
    }
    
    public Task<string?> GetStringAsync(string key)
    {
        var result = memoryCache.Get<string>(key);
        return Task.FromResult(result);
    }

    public T? Get<T>(string key) where T : class
    {
        return memoryCache.Get<T>(key);
    }

    public Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry;
            
        memoryCache.Set(key, value, options);
        return Task.FromResult(true);
    }
    
    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry;
            
        memoryCache.Set(key, value, options);
        return Task.FromResult(true);
    }
    
    public Task<bool> RemoveAsync(string key)
    {
        memoryCache.Remove(key);
        return Task.FromResult(true);
    }
    
    public Task<bool> ExistsAsync(string key)
    {
        var exists = memoryCache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }
    
    public override string ToString() => "MemoryCache";
}