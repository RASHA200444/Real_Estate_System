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
                .HasMaxLength(10);

            builder.HasOne(x => x.User)
                .WithMany() // لو عندك Navigation في User خلّيها WithMany(u => u.PaymentCards)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.CardTokenEncrypted }).IsUnique();
        }
    }
}
