using otherServices.Models;

namespace otherServices.Repositories
{
    public class SavedPostRepository : GenericRepository<SavedPost>,ISavedPostRepository
    {
        private readonly AppDbContext2 _context;

        public SavedPostRepository(AppDbContext2 context) : base(context)
        {
            _context = context;
        }
    }
}
