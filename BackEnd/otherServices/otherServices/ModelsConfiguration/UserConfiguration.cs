using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.UserId);

            builder.Property(u => u.UserName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(u => u.Email)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.HasIndex(u => u.Email)
                   .IsUnique();

            builder.Property(u => u.Password)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(u => u.RoleName)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(u => u.CreatedAt)
                   .HasDefaultValueSql("GETDATE()");

            // 🔹 Relations (1-to-1)
            //builder.HasOne(u => u.OwnerProfile)
            //       .WithOne(l => l.User)
            //       .HasForeignKey<Landlord>(l => l.UserId);

            //builder.HasOne(u => u.AdminProfile)
            //       .WithOne(a => a.User)
            //       .HasForeignKey<Admin>(a => a.UserId);

            // 🔹 Relations (1-to-Many)
            builder.HasMany(u => u.Comments)
                   .WithOne(c => c.User)
                   .HasForeignKey(c => c.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.SentMessages)
                   .WithOne(m => m.Sender)
                   .HasForeignKey(m => m.SenderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.ReceivedMessages)
                   .WithOne(m => m.Receiver)
                   .HasForeignKey(m => m.ReceiverId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.SavedPosts)
                   .WithOne(sp => sp.User)
                   .HasForeignKey(sp => sp.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Proposals)
                   .WithOne(p => p.User)
                   .HasForeignKey(p => p.TenantId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
