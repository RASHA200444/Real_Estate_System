using Microsoft.EntityFrameworkCore;
using otherServices.Models;

namespace otherServices.Repositories
{
    public class LikeRepository : GenericRepository<Like>, ILikeRepository
    {
        private readonly AppDbContext2 _context;

        public LikeRepository(AppDbContext2 context) : base(context)
        {
            _context = context;
        }

        public async Task<Like> GetByIdAsync(long userId, long postId)
        {
            return await _context.Likes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.PostId == postId);
        }

        public async Task<bool> ExistsAsync(long userId, long postId)
        {
            return await _context.Likes
                .AnyAsync(l => l.UserId == userId && l.PostId == postId);
        }
    }

}
