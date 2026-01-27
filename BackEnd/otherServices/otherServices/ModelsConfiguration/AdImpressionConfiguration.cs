using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class AdImpressionConfiguration : IEntityTypeConfiguration<AdImpression>
    {
        public void Configure(EntityTypeBuilder<AdImpression> builder)
        {
            builder.HasKey(x => x.AdImpressionId);

            builder.HasOne(x => x.Advertisement)
                .WithMany()
                .HasForeignKey(x => x.AdvertisementId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.AdvertisementId, x.UserId, x.DateKey });
        }
    }
}
