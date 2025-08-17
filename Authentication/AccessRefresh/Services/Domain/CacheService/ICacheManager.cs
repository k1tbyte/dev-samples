namespace AccessRefresh.Services.Domain.CacheService;

public interface ICacheManager
{
    Task<T?> GetAsync<T>(string key) where T : class;
    T? Get<T>(string key) where T : class;
    Task<string?> GetStringAsync(string key);
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<bool> RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);

    ICacheProvider GetCurrentCacheProvider();
    bool IsUsingFallback { get; }
}