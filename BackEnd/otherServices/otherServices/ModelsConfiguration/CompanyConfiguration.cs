using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(c => c.UserId);

        builder.Property(c => c.CompanyName)
               .IsRequired()
               .HasMaxLength(255);

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
