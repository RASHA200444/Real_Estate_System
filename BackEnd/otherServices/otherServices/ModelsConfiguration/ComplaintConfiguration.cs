using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
    {
        public void Configure(EntityTypeBuilder<Complaint> builder)
        {
            builder.ToTable("Complaints");

            builder.HasKey(c => c.ComplaintId);

            builder.Property(c => c.Content)
                   .IsRequired();

            // ✅ enum -> int
            builder.Property(c => c.Type)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(c => c.Status)
                   .HasConversion<int>()
                   .HasDefaultValue(Models.Enums.ComplaintStatus.Pending);

            builder.Property(c => c.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            // ✅ NEW AI moderation/report fields
            builder.Property(c => c.AiSeverity)
                   .IsRequired(false);

            builder.Property(c => c.AiReason)
                   .HasMaxLength(1000)
                   .IsRequired(false);

            builder.Property(c => c.AiAssessedAt)
                   .IsRequired(false);

            builder.Property(c => c.ReporterUserId).IsRequired();
            builder.Property(c => c.ReportedUserId).IsRequired();

            builder.HasOne(c => c.ReporterUser)
                   .WithMany(u => u.ComplaintsReported)
                   .HasForeignKey(c => c.ReporterUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.ReportedUser)
                   .WithMany(u => u.ComplaintsAgainst)
                   .HasForeignKey(c => c.ReportedUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.ReporterUserId, c.ReportedUserId });
        }
    }
}
