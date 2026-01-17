using otherServices.Models;

namespace otherServices.Repositories
{
    public class CommentRepository : GenericRepository<Comment>,ICommentRepository
    {
        private readonly AppDbContext2 _context;

        public CommentRepository(AppDbContext2 context) : base(context)
        {
            _context = context;
        }
    }
}
