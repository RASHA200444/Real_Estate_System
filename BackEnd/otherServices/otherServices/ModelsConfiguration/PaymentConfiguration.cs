using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payment");

            builder.HasKey(p => p.PaymentId);

            builder.Property(p => p.Amount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(p => p.PaidAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            // Payment → Transaction (Cascade ✅)
            builder.HasOne(p => p.Transaction)
                   .WithMany(t => t.Payments)
                   .HasForeignKey(p => p.TransactionId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Payment → CreditCard (Cascade ✅)
            builder.HasOne(p => p.CreditCard)
                   .WithMany()
                   .HasForeignKey(p => p.CreditCardId)
                   .OnDelete(DeleteBehavior.Cascade);

            //// ❌ Payment → User (NO CASCADE)
            //builder.HasOne(p => p.User)
            //       .WithMany()
            //       .HasForeignKey(p => p.UserId)
            //       .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
