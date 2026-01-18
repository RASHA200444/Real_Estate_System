using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class PaymentPlanConfiguration : IEntityTypeConfiguration<PaymentPlan>
    {
        public void Configure(EntityTypeBuilder<PaymentPlan> builder)
        {
            builder.HasKey(x => x.PaymentPlanId);

            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.PeriodicAmount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.PlatformFeePercent).HasColumnType("decimal(5,2)");

            builder.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Post)
                   .WithMany()
                   .HasForeignKey(x => x.PostId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PayerUser)
                   .WithMany()
                   .HasForeignKey(x => x.PayerUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PayeeUser)
                   .WithMany()
                   .HasForeignKey(x => x.PayeeUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PaymentCard)
                   .WithMany()
                   .HasForeignKey(x => x.PaymentCardId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
