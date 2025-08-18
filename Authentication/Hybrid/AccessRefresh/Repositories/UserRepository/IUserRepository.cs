using AccessRefresh.Data.Entities;
using AccessRefresh.Repositories.Base;

namespace AccessRefresh.Repositories.UserRepository;

public interface IUserRepository : IAsyncCrudRepository<User, IUserRepository>
{
    
}