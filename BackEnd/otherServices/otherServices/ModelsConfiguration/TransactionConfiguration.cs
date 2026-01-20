using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(t => t.TransactionId);

           

            builder.Property(t => t.Amount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();


            // CreatedAt default
            builder.Property(t => t.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");
            builder.Property(t => t.PaymentMethod)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(t => t.ExternalRef)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.HasIndex(t => t.ExternalRef).IsUnique();

            builder.Property(t => t.Attempts).HasDefaultValue(0);

            builder.Property(t => t.LastError).HasMaxLength(500);

            builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(t => t.Post)
                   .WithMany(p => p.Transactions)
                   .HasForeignKey(t => t.PostId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.User)
                   .WithMany(u => u.Transactions)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.PaymentSchedule)
                   .WithOne(s => s.Transaction)
                   .HasForeignKey<Transaction>(t => t.PaymentScheduleId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
