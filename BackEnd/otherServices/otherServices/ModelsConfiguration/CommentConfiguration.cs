using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.ToTable("Comments");

            // Primary Key
            builder.HasKey(c => c.CommentId);

            // Properties
            builder.Property(c => c.CommentId)
                   .HasColumnName("CommentId");

            builder.Property(c => c.PostId)
                   .IsRequired()
                   .HasColumnName("PostId");

            builder.Property(c => c.UserId)
                   .IsRequired()
                   .HasColumnName("UserId");

            builder.Property(c => c.Description)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(c => c.CreatedAt)
                   .HasColumnType("datetime")
                   .HasColumnName("CreatedAt");

            // Relationships
            builder.HasOne(c => c.Post)
                   .WithMany(p => p.Comments)   // Ensure Post has ICollection<Comment>
                   .HasForeignKey(c => c.PostId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.User)
                   .WithMany(u => u.Comments)  // Ensure User has ICollection<Comment>
                   .HasForeignKey(c => c.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
