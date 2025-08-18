namespace AccessRefresh.Services.Domain.CacheService;

public interface ICacheProvider
{
    bool IsConnected { get; }
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<string?> GetStringAsync(string key);
    T? Get<T>(string key) where T : class;
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<bool> RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}