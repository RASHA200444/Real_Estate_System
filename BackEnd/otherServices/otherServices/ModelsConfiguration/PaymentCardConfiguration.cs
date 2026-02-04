using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class PaymentCardConfiguration : IEntityTypeConfiguration<PaymentCard>
    {
        public void Configure(EntityTypeBuilder<PaymentCard> builder)
        {
            builder.ToTable("PaymentCards");

            builder.HasKey(x => x.PaymentCardId);

            builder.Property(x => x.CardTokenEncrypted)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.MaskedCardNumber)
                .IsRequired()
                .HasMaxLength(30);

            // ✅ enum -> int
            builder.Property(x => x.CardType).HasConversion<int>();

            // ✅ NEW: fraud fields
            builder.Property(x => x.FraudScore).IsRequired(false);

            builder.Property(x => x.FraudReason)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(x => x.FraudAssessedAt)
                   .IsRequired(false);

            builder.HasOne(x => x.User)
                .WithMany() // لو عندك User.PaymentCards اعملها: WithMany(u => u.PaymentCards)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.CardTokenEncrypted }).IsUnique();
        }
    }
}
