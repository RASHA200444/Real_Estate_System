using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
    {
        public void Configure(EntityTypeBuilder<Proposal> builder)
        {
            builder.ToTable("Proposals");

            builder.HasKey(p => p.ProposalId);

            builder.Property(p => p.Phone)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(p => p.FilePath)
                   .HasMaxLength(255)
                   .IsRequired();

            // ✅ enums -> int
            builder.Property(p => p.ProposalStatus).HasConversion<int>().IsRequired();
            builder.Property(p => p.IsInstallment).HasConversion<int>().IsRequired();
            builder.Property(p => p.IsAble).HasConversion<int>().IsRequired();
            builder.Property(p => p.RentIsAble).HasConversion<int>().IsRequired();

            builder.Property(p => p.StartRentalDate)
                   .HasColumnType("date")
                   .IsRequired(false);

            builder.Property(p => p.EndRentalDate)
                   .HasColumnType("date")
                   .IsRequired(false);

            // Installment eligibility fields
            builder.Property(p => p.EligibilityAnswersJson)
                   .HasColumnType("nvarchar(max)")
                   .IsRequired(false);

            builder.Property(p => p.EligibilityReason)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(p => p.EligibilityScore)
                   .IsRequired(false);

            builder.Property(p => p.EligibilityAssessedAt)
                   .IsRequired(false);

            // ✅ NEW: rent eligibility details
            builder.Property(p => p.RentEligibilityScore)
                   .IsRequired(false);

            builder.Property(p => p.RentEligibilityReason)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(p => p.RentEligibilityAssessedAt)
                   .IsRequired(false);

            builder.HasOne(p => p.User)
                   .WithMany(u => u.Proposals)
                   .HasForeignKey(p => p.TenantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Post)
                   .WithMany(pst => pst.Proposals)
                   .HasForeignKey(p => p.PostId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
