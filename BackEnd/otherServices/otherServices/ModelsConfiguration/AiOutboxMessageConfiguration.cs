using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class AiOutboxMessageConfiguration : IEntityTypeConfiguration<AiOutboxMessage>
    {
        public void Configure(EntityTypeBuilder<AiOutboxMessage> builder)
        {
            builder.ToTable("AiOutboxMessages");

            builder.HasKey(x => x.AiOutboxMessageId);

            builder.Property(x => x.RequestId)
                   .HasMaxLength(64)
                   .IsRequired();

            builder.Property(x => x.RequestType)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.EntityType)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.EnvelopeJson)
                   .HasColumnType("nvarchar(max)")
                   .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.LastError)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            builder.HasIndex(x => x.RequestId).IsUnique();
            builder.HasIndex(x => x.SentAtUtc);
            builder.HasIndex(x => x.CreatedAtUtc);

            // Useful composite index for queries
            builder.HasIndex(x => new { x.EntityType, x.EntityId, x.RequestType });
        }
    }
}
