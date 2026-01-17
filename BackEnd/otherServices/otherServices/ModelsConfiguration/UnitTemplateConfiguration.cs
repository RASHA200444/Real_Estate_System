//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using otherServices.Models;

//namespace otherServices.ModelsConfiguration
//{
//    public class UnitTemplateConfiguration : IEntityTypeConfiguration<UnitTemplate>
//    {
//        public void Configure(EntityTypeBuilder<UnitTemplate> builder)
//        {
//            builder.ToTable("UnitTemplates");

//            builder.HasKey(x => x.UnitTemplateId);

//            builder.Property(x => x.UnitCode)
//                   .IsRequired()
//                   .HasMaxLength(10);

//            builder.Property(x => x.Title)
//                   .IsRequired()
//                   .HasMaxLength(255);

//            builder.Property(x => x.Description)
//                   .IsRequired()
//                   .HasMaxLength(1000);

//            builder.HasOne(x => x.Project)
//                   .WithMany(p => p.UnitTemplates)
//                   .HasForeignKey(x => x.ProjectId)
//                   .OnDelete(DeleteBehavior.Cascade);
//        }
//    }
//}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

public class UnitTemplateConfiguration : IEntityTypeConfiguration<UnitTemplate>
{
    public void Configure(EntityTypeBuilder<UnitTemplate> builder)
    {
        builder.ToTable("UnitTemplates");

        builder.HasKey(x => x.UnitTemplateId);

        builder.Property(x => x.UnitCode)
               .IsRequired()
               .HasMaxLength(10);

        builder.Property(x => x.Title)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(x => x.Description)
               .IsRequired()
               .HasMaxLength(1000);

        builder.HasOne(x => x.Project)
               .WithMany(p => p.UnitTemplates)
               .HasForeignKey(x => x.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        // منع تكرار UnitCode داخل نفس Project
        builder.HasIndex(x => new { x.ProjectId, x.UnitCode }).IsUnique();
    }
}
