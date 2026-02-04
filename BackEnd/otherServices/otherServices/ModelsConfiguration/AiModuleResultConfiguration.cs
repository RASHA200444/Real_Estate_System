using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class AiModuleResultConfiguration : IEntityTypeConfiguration<AiModuleResult>
    {
        public void Configure(EntityTypeBuilder<AiModuleResult> builder)
        {
            builder.ToTable("AiModuleResults");

            builder.HasKey(x => x.AiModuleResultId);

            builder.Property(x => x.RequestId)
                   .HasMaxLength(64)
                   .IsRequired();

            builder.Property(x => x.RequestType)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.EntityType)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.Reason)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            builder.Property(x => x.PayloadJson)
                   .HasColumnType("nvarchar(max)")
                   .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.ProcessedAtUtc)
                   .IsRequired(false);

            builder.HasIndex(x => x.RequestId).IsUnique();
            builder.HasIndex(x => new { x.EntityType, x.EntityId, x.RequestType });
            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
