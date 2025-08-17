using AccessRefresh.Contracts.DTOs;
using AccessRefresh.Data.Entities;
using AccessRefresh.Repositories.UserRepository;
using AccessRefresh.Services.Domain.CacheService;
using Mapster;

namespace AccessRefresh.Services.Application;

public sealed class UserService(IUserRepository repository, ICacheManager cache)
{
    public async Task<User?> GetUserById(int id)
    {
        var key = $"user:{id}";
        var cached = await cache.GetAsync<User>(key);
        if (cached != null)
        {
            return cached;
        }
        
        var entry = await repository.Set.FindAsync(id);

        if (entry == null)
        {
            return null;
        }
        
        await cache.SetAsync(key, entry, TimeSpan.FromHours(1));
        return entry;
    }
}