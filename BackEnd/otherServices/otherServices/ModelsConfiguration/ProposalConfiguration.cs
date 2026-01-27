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

            builder.Property(p => p.ProposalStatus)
                   .IsRequired();

            builder.Property(p => p.StartRentalDate)
                   .HasColumnType("date")
                   .IsRequired(false);

            builder.Property(p => p.EndRentalDate)
                   .HasColumnType("date")
                   .IsRequired(false);

            // ✅ eligibility fields
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
