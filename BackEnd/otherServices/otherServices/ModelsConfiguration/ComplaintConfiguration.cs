using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
    {
        public void Configure(EntityTypeBuilder<Complaint> builder)
        {
            // Primary Key
            builder.HasKey(c => c.ComplaintId);

            // Properties
            builder.Property(c => c.Content)
                   .IsRequired();

            builder.Property(c => c.Type)
                   .IsRequired();

            builder.Property(c => c.Status)
                   .HasDefaultValue(Models.Enums.ComplaintStatus.Pending);

            builder.Property(c => c.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            // Foreign keys
            builder.Property(c => c.ReporterUserId).IsRequired();
            builder.Property(c => c.ReportedUserId).IsRequired();

            // Relationships
            builder.HasOne(c => c.ReporterUser)
                   .WithMany(u => u.ComplaintsReported)
                   .HasForeignKey(c => c.ReporterUserId)
                   .OnDelete(DeleteBehavior.Restrict); // مهم لتجنب Multiple Cascade Paths

            builder.HasOne(c => c.ReportedUser)
                   .WithMany(u => u.ComplaintsAgainst)
                   .HasForeignKey(c => c.ReportedUserId)
                   .OnDelete(DeleteBehavior.Restrict); // مهم لتجنب Multiple Cascade Paths

            // Optional: index لتحسين الاستعلامات
            builder.HasIndex(c => new { c.ReporterUserId, c.ReportedUserId });
        }
    }
}
