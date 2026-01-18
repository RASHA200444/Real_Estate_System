using otherServices.Models;

namespace otherServices.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext2 _context;

        public IGenericRepository<User> Users { get; }
        public IGenericRepository<Proposal> Proposals { get; }
        public IGenericRepository<Post> Posts { get; }
        public IGenericRepository<PostImage> PostImages { get; }
        public IGenericRepository<Comment> Comments { get; }
        public IGenericRepository<Transaction> Transactions { get; }
        public IGenericRepository<SavedPost> SavedPosts { get; }
        public IGenericRepository<Notification> Notifications { get; }
        public IGenericRepository<Complaint> Complaints { get; }
        public IGenericRepository<Admin> Admins { get; }
        public IGenericRepository<Message> Messages { get; }

        public IGenericRepository<PaymentCard> PaymentCards { get; }
        public IGenericRepository<BankCard> BankCards { get; }
        public IGenericRepository<BankTokenMap> BankTokenMaps { get; }
        public IGenericRepository<PaymentPlan> PaymentPlans { get; }
        public IGenericRepository<PaymentSchedule> PaymentSchedules { get; }

        // ✅ NEW
        public IGenericRepository<Contract> Contracts { get; }
        public IGenericRepository<ContractSignature> ContractSignatures { get; }

        public IRatingsRepository Ratings { get; }
        public ILikeRepository Likes { get; }
        public ILandlordRepository Landlords { get; }

        public AppDbContext2 Context { get; }

        public UnitOfWork(AppDbContext2 context)
        {
            _context = context;
            Context = context;

            Users = new GenericRepository<User>(_context);
            Posts = new GenericRepository<Post>(_context);
            PostImages = new GenericRepository<PostImage>(_context);
            Comments = new GenericRepository<Comment>(_context);
            Transactions = new GenericRepository<Transaction>(_context);
            SavedPosts = new GenericRepository<SavedPost>(_context);
            Notifications = new GenericRepository<Notification>(_context);
            Complaints = new GenericRepository<Complaint>(_context);
            Admins = new GenericRepository<Admin>(_context);
            Messages = new GenericRepository<Message>(_context);

            PaymentCards = new GenericRepository<PaymentCard>(_context);
            BankCards = new GenericRepository<BankCard>(_context);
            BankTokenMaps = new GenericRepository<BankTokenMap>(_context);
            PaymentPlans = new GenericRepository<PaymentPlan>(_context);
            PaymentSchedules = new GenericRepository<PaymentSchedule>(_context);

            // ✅ NEW
            Contracts = new GenericRepository<Contract>(_context);
            ContractSignatures = new GenericRepository<ContractSignature>(_context);

            Likes = new LikeRepository(_context);
            Landlords = new LandlordRepository(_context);
            Ratings = new RatingsRepository(_context);
        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
        public void Dispose() => _context.Dispose();
    }
}
