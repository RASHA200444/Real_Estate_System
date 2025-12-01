using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class PostImageConfiguration : IEntityTypeConfiguration<PostImage>
    {
        public void Configure(EntityTypeBuilder<PostImage> builder)
        {
            builder.ToTable("PostImages");

            // Primary Key
            builder.HasKey(pi => pi.ImageId);

            // Properties
            builder.Property(pi => pi.ImageUrl)
                   .HasMaxLength(500)
                   .IsRequired();

            // Relationships
            builder.HasOne(pi => pi.Post)
                   .WithMany(p => p.PostImages)
                   .HasForeignKey(pi => pi.PostId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
