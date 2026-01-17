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

            // Primary Key
            builder.HasKey(l => l.LandlordId);

            // Properties
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

            builder.Property(l => l.PendingStatus)
                   .HasConversion<int>();

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
