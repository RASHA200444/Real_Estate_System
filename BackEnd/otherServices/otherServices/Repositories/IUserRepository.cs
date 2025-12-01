using otherServices.Models;
namespace otherServices.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetWithRolesAsync(int id);  // لو عندك علاقات Roles أو Profiles
    }
}
