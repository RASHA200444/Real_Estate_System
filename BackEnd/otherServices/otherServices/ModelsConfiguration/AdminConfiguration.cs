using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class AdminConfiguration : IEntityTypeConfiguration<Admin>
    {
        public void Configure(EntityTypeBuilder<Admin> builder)
        {
            builder.ToTable("Admins");

            // Primary Key
            builder.HasKey(a => a.AdminId);

            // Properties
            builder.Property(a => a.AdminId)
                   .HasColumnName("AdminId");

            builder.Property(a => a.UserId)
                   .IsRequired()
                   .HasColumnName("UserId");

            builder.Property(a => a.Type)
                   .IsRequired()
                   .HasConversion<int>(); // Enum to int

            builder.Property(a => a.PrivilegeType)
                   .IsRequired()
                   .HasConversion<int>(); // Enum to int

            // Relationships
            builder.HasOne(a => a.User)
                   .WithOne(u => u.Admin)  // One-to-One
                   .HasForeignKey<Admin>(a => a.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
