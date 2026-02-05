using otherServices.Models;
using otherServices.Models.Enums;

namespace otherServices.Repositories
{
    public class PostRepository :GenericRepository<Post>, IPostRepository
    {
        private readonly AppDbContext2 _context;

        public PostRepository(AppDbContext2 context) : base(context)
        {
            _context = context;
        }

        public async Task<Post> AcceptPostAsync(long postId)
        {
            var post = await GetByIdAsync(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            post.PendingStatus = PostPendingStatus.Accepted;
            post.IsAdminFinalized = true;
            post.AdminFinalizedAtUtc = DateTime.UtcNow;

            await SaveChangesAsync();
            return post;
        }

        public async Task<Post> RejectPostAsync(long postId)
        {
            var post = await GetByIdAsync(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            post.PendingStatus = PostPendingStatus.Refused;
            post.IsAdminFinalized = true;
            post.AdminFinalizedAtUtc = DateTime.UtcNow;

            await SaveChangesAsync();
            return post;
        }

    }
}
