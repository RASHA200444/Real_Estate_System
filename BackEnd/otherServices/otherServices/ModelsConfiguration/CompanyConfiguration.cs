using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");

            builder.HasKey(c => c.UserId);

            builder.Property(c => c.CompanyName)
                   .IsRequired()
                   .HasMaxLength(255);

            // ✅ enum conversions
            builder.Property(c => c.CommercialRegisterEvaluation).HasConversion<int>();
            builder.Property(c => c.PendingStatus).HasConversion<int>();

            builder.Property(c => c.CommercialRegisterPath)
                   .IsRequired(false);

            // ✅ NEW anomaly fields
            builder.Property(c => c.AnomalyScore).IsRequired(false);

            builder.Property(c => c.AnomalyReason)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(c => c.AnomalyFlaggedAt).IsRequired(false);

            builder.HasOne(c => c.User)
                   .WithOne()
                   .HasForeignKey<Company>(c => c.UserId);

            builder.HasMany(c => c.Projects)
                   .WithOne(p => p.Company)
                   .HasForeignKey(p => p.CompanyId);

            builder.HasOne(c => c.Landlord)
                   .WithMany()
                   .HasForeignKey(c => c.LandlordId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
