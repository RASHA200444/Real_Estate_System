using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(x => x.RefreshTokenId);

            builder.Property(x => x.TokenHash)
                   .IsRequired()
                   .HasMaxLength(256);

            builder.Property(x => x.ReplacedByTokenHash)
                   .HasMaxLength(256)
                   .IsRequired(false);

            builder.Property(x => x.CreatedByIp)
                   .HasMaxLength(64)
                   .IsRequired(false);

            builder.Property(x => x.RevokedByIp)
                   .HasMaxLength(64)
                   .IsRequired(false);

            builder.Property(x => x.UserAgent)
                   .HasMaxLength(512)
                   .IsRequired(false);

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ExpiresAt).IsRequired();
            builder.Property(x => x.RevokedAt).IsRequired(false);

            builder.HasOne(x => x.User)
                   .WithMany() // لو عايز: User.RefreshTokens => غيّرها لاحقًا
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // indexes
            builder.HasIndex(x => new { x.UserId, x.TokenHash }).IsUnique();
            builder.HasIndex(x => x.ExpiresAt);
        }
    }
}
