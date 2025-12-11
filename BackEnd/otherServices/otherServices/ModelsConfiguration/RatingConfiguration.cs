using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class RatingConfiguration : IEntityTypeConfiguration<Rating>
    {
        public void Configure(EntityTypeBuilder<Rating> builder)
        {
            builder.ToTable("Ratings");

            builder.HasKey(r => r.RatingId);

            builder.Property(r => r.Score)
                   .HasColumnType("decimal(3,2)")
                   .IsRequired();

            builder.Property(r => r.Comment)
                   .HasMaxLength(255);

            builder.Property(r => r.CreatedAt)
                   .HasColumnType("datetime")
                   .HasDefaultValueSql("GETDATE()");

            builder.HasOne(r => r.Landlord)
                   .WithMany(l => l.Ratings)
                   .HasForeignKey(r => r.LandlordId)
                   .OnDelete(DeleteBehavior.Cascade)
                   .HasConstraintName("FK_Rating_Landlord");

            builder.HasOne(r => r.RaterUser)
                   .WithMany(u => u.Ratings) 
                   .HasForeignKey(r => r.RaterId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .HasConstraintName("FK_Rating_RaterUser");
        }
    }
}
