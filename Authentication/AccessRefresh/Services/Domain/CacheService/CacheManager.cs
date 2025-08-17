namespace AccessRefresh.Services.Domain.CacheService;

public class CacheManager : ICacheManager
{
    private readonly ICacheProvider _primaryCache;
    private readonly ICacheProvider _fallbackCache;

    public CacheManager(
        [FromKeyedServices("primary")] ICacheProvider primaryCache,
        [FromKeyedServices("fallback")] ICacheProvider fallbackCache,
        ILogger<CacheManager> logger)
    {
        _primaryCache = primaryCache;
        _fallbackCache = fallbackCache;
        logger.LogInformation("Now using: {provider}", GetCurrentCacheProvider());
        
    }

    public ICacheProvider GetCurrentCacheProvider()
    {
        return _primaryCache.IsConnected ? _primaryCache : _fallbackCache;
    }
    
    public Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            return GetCurrentCacheProvider().GetAsync<T>(key);
        }
        catch 
        {
            return Task.FromResult<T?>(null);
        }
    }

    public T? Get<T>(string key) where T : class
    {
        return GetCurrentCacheProvider().Get<T>(key);
    }

    public Task<string?> GetStringAsync(string key)
    {
        return GetCurrentCacheProvider().GetStringAsync(key);
    }

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            return await GetCurrentCacheProvider().SetStringAsync(key, value, expiry);
        }
        catch
        {
            return false;
        }
    }
    
    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            return  GetCurrentCacheProvider().SetAsync(key, value, expiry);
        }
        catch 
        {
            return Task.FromResult(false);
        }
    }
    
    public Task<bool> RemoveAsync(string key)
    {
        try
        {
            return  GetCurrentCacheProvider().RemoveAsync(key);
        }
        catch 
        {
            return Task.FromResult(false);
        }
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await GetCurrentCacheProvider().ExistsAsync(key);
        }
        catch 
        {
            return false;
        }
    }
    
    public bool IsUsingFallback => !_primaryCache.IsConnected;
}