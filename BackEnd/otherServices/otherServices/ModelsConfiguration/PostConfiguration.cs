using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.ToTable("Posts");

            builder.HasKey(p => p.PostId);

            builder.Property(p => p.Title)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(p => p.Description)
                   .IsRequired()
                   .HasMaxLength(1000);

            builder.Property(p => p.PostDocPath)
                   .IsRequired();


            builder.Property(p => p.Location)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(p => p.CreatedAt)
                   .HasDefaultValueSql("GETDATE()");

            builder.HasOne(p => p.Landlord)
                   .WithMany(l => l.Posts)
                   .HasForeignKey(p => p.LandlordId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Comments)
                   .WithOne(c => c.Post)
                   .HasForeignKey(c => c.PostId);

            builder.HasMany(p => p.SavedPosts)
                   .WithOne(sp => sp.Post)
                   .HasForeignKey(sp => sp.PostId);

            builder.HasMany(p => p.Proposals)
                   .WithOne(pr => pr.Post)
                   .HasForeignKey(pr => pr.PostId);

            builder.HasMany(p => p.Likes)
                   .WithOne(l => l.Post)
                   .HasForeignKey(l => l.PostId);

            builder.Property(p => p.TagsJson)
                   .HasColumnName("TagsJson")
                   .HasColumnType("nvarchar(max)")
                   .IsRequired(false);


        }
    }
}
