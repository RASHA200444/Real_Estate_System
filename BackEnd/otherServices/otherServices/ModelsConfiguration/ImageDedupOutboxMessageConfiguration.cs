// ModelsConfiguration/ImageDedupOutboxMessageConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration;

public class ImageDedupOutboxMessageConfiguration
    : IEntityTypeConfiguration<ImageDedupOutboxMessage>
{
    public void Configure(EntityTypeBuilder<ImageDedupOutboxMessage> builder)
    {
        builder.ToTable("ImageDedupOutboxMessages");

        builder.HasKey(x => x.ImageDedupOutboxMessageId);

        builder.Property(x => x.ImageId)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.PostId)
               .IsRequired();

        builder.Property(x => x.EnvelopeJson)
               .IsRequired();

        builder.Property(x => x.LastError)
               .HasMaxLength(2000);

        // Index: fast poll for unsent messages
        builder.HasIndex(x => x.SentAtUtc);
        builder.HasIndex(x => x.PostId);
    }
}
