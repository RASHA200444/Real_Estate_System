using Microsoft.EntityFrameworkCore;
using otherServices.Models;

namespace otherServices.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly AppDbContext2 _context;

        public UserRepository(AppDbContext2 context) : base(context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetWithRolesAsync(int id)
        {
            return await _context.Users
                .Include(u => u.RoleName)             // لو عندك جدول Roles
                                                   //.Include(u => u.OwnerProfile)    // لو عندك علاقات تانية
                                                   //.Include(u => u.AdminProfile)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }
    }
}