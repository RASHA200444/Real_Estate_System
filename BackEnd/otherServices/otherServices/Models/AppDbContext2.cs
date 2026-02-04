using Microsoft.EntityFrameworkCore;
using otherServices.ModelsConfiguration;
using otherServices.ConfigurationModels;

namespace otherServices.Models
{
    public class AppDbContext2 : DbContext
    {
        public AppDbContext2(DbContextOptions<AppDbContext2> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Landlord> Landlords { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostImage> PostImages { get; set; }
        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<SavedPost> SavedPosts { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<UnitTemplate> UnitTemplates { get; set; }

        public DbSet<PaymentCard> PaymentCards { get; set; }
        public DbSet<BankCard> BankCards { get; set; }
        public DbSet<BankTokenMap> BankTokenMaps { get; set; }
        public DbSet<PaymentPlan> PaymentPlans { get; set; }
        public DbSet<PaymentSchedule> PaymentSchedules { get; set; }

        // ✅ Contracts
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractSignature> ContractSignatures { get; set; }

        // Ads
        public DbSet<Advertisement> Advertisements { get; set; }
        public DbSet<AdImpression> AdImpressions { get; set; }

        // Refresh Tokens
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // ✅ NEW: Generic table for ALL AI modules (29)
        public DbSet<AiModuleResult> AiModuleResults { get; set; }
        // ✅ NEW: Outbox table for AI requests
        public DbSet<AiOutboxMessage> AiOutboxMessages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new AdminConfiguration());
            modelBuilder.ApplyConfiguration(new LandlordConfiguration());
            modelBuilder.ApplyConfiguration(new CommentConfiguration());
            modelBuilder.ApplyConfiguration(new PostConfiguration());
            modelBuilder.ApplyConfiguration(new PostImageConfiguration());
            modelBuilder.ApplyConfiguration(new ProposalConfiguration());
            modelBuilder.ApplyConfiguration(new RatingConfiguration());
            modelBuilder.ApplyConfiguration(new SavedPostConfiguration());
            modelBuilder.ApplyConfiguration(new LikeConfiguration());
            modelBuilder.ApplyConfiguration(new MessageConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationConfiguration());
            modelBuilder.ApplyConfiguration(new ComplaintConfiguration());
            modelBuilder.ApplyConfiguration(new TransactionConfiguration());
            modelBuilder.ApplyConfiguration(new SubscriptionPlanConfiguration());
            modelBuilder.ApplyConfiguration(new UserSubscriptionConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyConfiguration());
            modelBuilder.ApplyConfiguration(new ProjectConfiguration());
            modelBuilder.ApplyConfiguration(new UnitTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentCardConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentPlanConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentScheduleConfiguration());

            modelBuilder.ApplyConfiguration(new ContractConfiguration());
            modelBuilder.ApplyConfiguration(new ContractSignatureConfiguration());

            modelBuilder.ApplyConfiguration(new AdvertisementConfiguration());
            modelBuilder.ApplyConfiguration(new AdImpressionConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());

            // ✅ NEW
            modelBuilder.ApplyConfiguration(new AiModuleResultConfiguration());
            modelBuilder.ApplyConfiguration(new AiOutboxMessageConfiguration());

        }
    }
}
