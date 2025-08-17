using AccessRefresh.Data.Context;
using AccessRefresh.Data.Entities;
using AccessRefresh.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace AccessRefresh.Repositories.SessionRepository;

public sealed class SessionRepository(AppDbContext context) :
        BaseAsyncCrudRepository<Session, ISessionRepository>(context, context.Sessions), ISessionRepository
{
    public Task EnsureSessionLimitAsync(int userId, int maxSessions = 5)
    {
        var sessions = context.Sessions
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.ExpiresAt)
            .Skip(maxSessions);

        return sessions.Any() ? sessions.ExecuteDeleteAsync() : Task.CompletedTask;
    }

    public Guid[] GetSessionIdsByUserIdAsync(int userId)
    {
        return context.Sessions
            .Where(o => o.UserId == userId)
            .Select(o => o.SessionId)
            .ToArray();
    }
}