using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
    {
        public void Configure(EntityTypeBuilder<UserSubscription> builder)
        {
            builder.ToTable("UserSubscriptions");

            // Primary Key
            builder.HasKey(us => us.UserSubscriptionId);

            // Properties
            builder.Property(us => us.StartDate)
                   .IsRequired();

            builder.Property(us => us.EndDate)
                   .IsRequired();

            builder.Property(us => us.Status)
                   .IsRequired()
                   .HasConversion<int>();

            builder.Property(us => us.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            // =======================
            // Relationships
            // =======================

            // User ↔ UserSubscription (One-to-One)
            builder.HasOne(us => us.User)
                   .WithOne(u => u.UserSubscription) // بدل WithMany
                   .HasForeignKey<UserSubscription>(us => us.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
            // ❗ Restrict مهم لتجنب multiple cascade paths

            // SubscriptionPlan (One Plan → Many UserSubscriptions)
            builder.HasOne(us => us.SubscriptionPlan)
                   .WithMany(sp => sp.UserSubscriptions)
                   .HasForeignKey(us => us.SubscriptionPlanId)
                   .OnDelete(DeleteBehavior.Restrict);
            // ❗ لا نمسح اشتراكات لو الأدمن لغى الخطة

            // Transaction (One-to-One optional)
            builder.HasOne(us => us.Transaction)
                   .WithOne(t => t.UserSubscription)
                   .HasForeignKey<UserSubscription>(us => us.TransactionId)
                   .OnDelete(DeleteBehavior.SetNull);
            // ✔ لو الترانزاكشن اتمسحت، الاشتراك يفضل موجود
        }
    }
}
