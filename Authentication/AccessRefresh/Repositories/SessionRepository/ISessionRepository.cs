using AccessRefresh.Data.Entities;
using AccessRefresh.Repositories.Base;

namespace AccessRefresh.Repositories.SessionRepository;

public interface ISessionRepository : IAsyncCrudRepository<Session, ISessionRepository>
{
    Task EnsureSessionLimitAsync(int userId, int maxSessions = 5);
    Guid[] GetSessionIdsByUserIdAsync(int userId);
}