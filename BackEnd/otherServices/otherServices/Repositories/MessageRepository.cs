using otherServices.Models;

namespace otherServices.Repositories
{
    public class MessageRepository : GenericRepository<Message>,IMessageRepository
    {
        private readonly AppDbContext2 _context;

        public MessageRepository(AppDbContext2 context) : base(context)
        {
            _context = context;
        }
    }
}
