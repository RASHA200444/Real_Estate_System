using otherServices.Models;

namespace otherServices.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<User> Users { get; }
        IGenericRepository<Proposal> Proposals { get; }
        IGenericRepository<Post> Posts { get; }
        IGenericRepository<PostImage> PostImages { get; }
        IGenericRepository<Comment> Comments { get; }
        IGenericRepository<Transaction> Transactions { get; }
        IGenericRepository<SavedPost> SavedPosts { get; }
        IGenericRepository<Notification> Notifications { get; }
        IGenericRepository<Complaint> Complaints { get; }
        IGenericRepository<Admin> Admins { get; }
        IGenericRepository<Message> Messages { get; }


        // 🔹 NEW
        IGenericRepository<SubscriptionPlan> SubscriptionPlans { get; }
        IGenericRepository<UserSubscription> UserSubscriptions { get; }


        IGenericRepository<PaymentCard> PaymentCards { get; }
        IGenericRepository<BankCard> BankCards { get; }
        IGenericRepository<BankTokenMap> BankTokenMaps { get; }
        IGenericRepository<PaymentPlan> PaymentPlans { get; }
        IGenericRepository<PaymentSchedule> PaymentSchedules { get; }

        // ✅ NEW
        IGenericRepository<Contract> Contracts { get; }
        IGenericRepository<ContractSignature> ContractSignatures { get; }

        IRatingsRepository Ratings { get; }
        ILikeRepository Likes { get; }
        ILandlordRepository Landlords { get; }

        IGenericRepository<Advertisement> Advertisements { get; }
        IGenericRepository<AdImpression> AdImpressions { get; }

        Task<int> CompleteAsync();
    }
}
