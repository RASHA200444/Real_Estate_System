using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.ProjectId);

        builder.Property(p => p.ProjectName)
               .IsRequired()
               .HasMaxLength(255);

        builder.HasMany(p => p.Posts)
               .WithOne(po => po.Project)
               .HasForeignKey(po => po.ProjectId);

        builder.HasMany(p => p.UnitTemplates)
               .WithOne(ut => ut.Project)
               .HasForeignKey(ut => ut.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Company)
               .WithMany(c => c.Projects)
               .HasForeignKey(p => p.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
