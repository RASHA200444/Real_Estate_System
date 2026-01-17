using otherServices.Models;

namespace otherServices.Repositories
{
    public class ProposalRepository : GenericRepository<Proposal>,IProposalRepository
    {
        private readonly AppDbContext2 _context;

        public ProposalRepository(AppDbContext2 context) : base(context)
        {
            _context = context;
        }
    }
}
