using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class SavedPostConfiguration : IEntityTypeConfiguration<SavedPost>
    {
        public void Configure(EntityTypeBuilder<SavedPost> builder)
        {
            builder.ToTable("SavedPost");

            builder.HasKey(sp => new { sp.UserId, sp.PostId });

            builder.Property(sp => sp.SavedDate)
                   .HasColumnType("datetime")
                   .HasDefaultValueSql("GETDATE()");

            builder.HasOne(sp => sp.User)
                   .WithMany(u => u.SavedPosts)
                   .HasForeignKey(sp => sp.UserId)
                   .OnDelete(DeleteBehavior.Cascade)
                   .HasConstraintName("FK_SavedPost_User");

            builder.HasOne(sp => sp.Post)
                   .WithMany(p => p.SavedPosts)
                   .HasForeignKey(sp => sp.PostId)
                   .OnDelete(DeleteBehavior.Cascade)
                   .HasConstraintName("FK_SavedPost_Post");
        }
    }
}
