using Microsoft.EntityFrameworkCore;

namespace AccessRefresh.Repositories.Base;

public class BaseAsyncCrudRepository<T, TDerived>(DbContext context, DbSet<T> set) : IAsyncCrudRepository<T, TDerived> 
    where T : class
    where TDerived : IAsyncCrudRepository<T, TDerived>
{
    int AutoSaveRequests { get; set; }
    public DbSet<T> Set => set;
    public TDerived WithAutoSaveNext(int nextRequestsCount = 1)
    {
        AutoSaveRequests = nextRequestsCount;
        return (TDerived)(object)this;
    }

    public async Task<T> Add(T entity)
    {
        var entry = await Set.AddAsync(entity);
        await _saveInternalAsync();
        return entry.Entity;
    }

    public async Task<T?> Get(int id)
    {
        return await Set.FindAsync(id);
    }

    public async Task<T> Update(T entity)
    {
        var entry = Set.Update(entity);
        await _saveInternalAsync();
        return entry.Entity;
    }

    public async Task<bool> DeleteById(object id)
    {
        var entity = await Set.FindAsync(id);
        return await Delete(entity);
    }

    public async Task<bool> Delete(T? entity)
    {
        if (entity == null)
            return false;

        Set.Remove(entity);
        await _saveInternalAsync();
        return true;
    }
    public async Task<TDerived> SaveAsync()
    {
        await context.SaveChangesAsync().ConfigureAwait(false);
        return (TDerived)(object)this;
    }
    
    
    private async Task _saveInternalAsync()
    {
        if (AutoSaveRequests == -1)
        {
            await SaveAsync();
            return;
        }
        if (AutoSaveRequests > 0)
        {
            AutoSaveRequests--;
            await SaveAsync();
        }
    }
}