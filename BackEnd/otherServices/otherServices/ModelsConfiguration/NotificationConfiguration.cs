using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            // Primary Key
            builder.HasKey(n => n.NotificationId);

            // Properties
            builder.Property(n => n.Content)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(n => n.ReadStatus)
                   .HasDefaultValue(false);

            builder.Property(n => n.CreatedAt)
                   .HasColumnType("datetime")
                   .HasDefaultValueSql("GETDATE()");

            // Relationships
            builder.HasOne(n => n.User)
                   .WithMany(u => u.Notifications)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
