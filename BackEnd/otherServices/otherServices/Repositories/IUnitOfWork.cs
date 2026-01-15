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
        IGenericRepository<CreditCard> CreditCards { get; }

        // 🔹 NEW
        IGenericRepository<Payment> Payments { get; }
        IGenericRepository<SubscriptionPlan> SubscriptionPlans { get; }
        IGenericRepository<UserSubscription> UserSubscriptions { get; }

        IRatingsRepository Ratings { get; }
        ILikeRepository Likes { get; }
        ILandlordRepository Landlords { get; }

        Task<int> CompleteAsync();
    }
}
