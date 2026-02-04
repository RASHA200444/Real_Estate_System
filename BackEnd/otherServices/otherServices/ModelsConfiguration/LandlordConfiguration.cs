using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class LandlordConfiguration : IEntityTypeConfiguration<Landlord>
    {
        public void Configure(EntityTypeBuilder<Landlord> builder)
        {
            builder.ToTable("Landlords");

            builder.HasKey(l => l.LandlordId);

            builder.Property(l => l.LandlordId)
                   .HasColumnName("LandlordId");

            builder.Property(l => l.UserId)
                   .IsRequired();

            builder.Property(l => l.Rate)
                   .HasColumnType("float");

            builder.Property(l => l.OwnershipDocPath)
                   .HasMaxLength(255);

            builder.Property(l => l.IsPro)
                   .HasColumnName("IsPro");

            // ✅ enum conversions
            builder.Property(l => l.PendingStatus).HasConversion<int>();
            builder.Property(l => l.OwnershipDocPathEvaluation).HasConversion<int>();
            builder.Property(l => l.ComPanStatus).HasConversion<int>();

            builder.Property(l => l.SuspendedUntil)
                   .IsRequired(false);

            // ✅ NEW anomaly fields
            builder.Property(l => l.AnomalyScore).IsRequired(false);

            builder.Property(l => l.AnomalyReason)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(l => l.AnomalyFlaggedAt).IsRequired(false);

            // Relationships
            builder.HasOne(l => l.User)
                   .WithOne(u => u.Landlord)
                   .HasForeignKey<Landlord>(l => l.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(l => l.Posts)
                   .WithOne(p => p.Landlord)
                   .HasForeignKey(p => p.LandlordId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(l => l.Ratings)
                   .WithOne(r => r.Landlord)
                   .HasForeignKey(r => r.LandlordId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
