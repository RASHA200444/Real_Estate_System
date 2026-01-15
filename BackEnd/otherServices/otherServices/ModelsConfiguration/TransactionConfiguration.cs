using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{

    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            // Primary Key
            builder.HasKey(t => t.TransactionId);

            // Amount Precision
            builder.Property(t => t.TotalAmount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            // CreatedAt default
            builder.Property(t => t.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            // Relationship: Transaction ↔ Post (many-to-one)
            builder.HasOne(t => t.Post)
                   .WithMany(p => p.Transactions)
                   .HasForeignKey(t => t.PostId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationship: Transaction ↔ User (many-to-one)
            builder.HasOne(t => t.User)
                   .WithMany(u => u.Transactions)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
