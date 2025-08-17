using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using AccessRefresh.Data.Entities;
using AccessRefresh.Services.Application.AuthService;
using Microsoft.Extensions.Caching.Distributed;

namespace AccessRefresh.Extensions;

public static class UtilExtensions
{
    public static User? GetUser(this HttpContext context)
    {
        return context.Items["user"] as User;
    }
    
    public static Session? GetSession(this HttpContext context)
    {
        return context.Items["session"] as Session;
    }

    public static string GetFingerprint(this HttpContext context)
    {
        return (context.Items["Fingerprint"] as string)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<T?> GetObjectAsync<T>(
        this IDistributedCache cache, 
        string key,
        CancellationToken token = default) where T : class
    {
        var json = await cache.GetStringAsync(key, token: token);
        if (json == null)
        {
            return null;
        }

        return  JsonSerializer.Deserialize<T>(json) ?? null;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task SetObjectAsync<T>(
        this IDistributedCache cache,
        string key, 
        T value, 
        DistributedCacheEntryOptions options, 
        CancellationToken token = default) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await cache.SetStringAsync(key, json, options, token);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task SetObjectAsync<T>(
        this IDistributedCache cache,
        string key, 
        T value, 
        CancellationToken token = default) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await cache.SetStringAsync(key, json, token);
    }
}