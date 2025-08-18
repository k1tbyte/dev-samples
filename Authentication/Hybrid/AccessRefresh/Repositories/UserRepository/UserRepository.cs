using AccessRefresh.Data.Context;
using AccessRefresh.Data.Entities;
using AccessRefresh.Repositories.Base;

namespace AccessRefresh.Repositories.UserRepository;

public sealed class UserRepository(AppDbContext context) :
    BaseAsyncCrudRepository<User, IUserRepository>(context, context.Users), IUserRepository
{
    
}