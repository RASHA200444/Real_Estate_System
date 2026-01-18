using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class PaymentScheduleConfiguration : IEntityTypeConfiguration<PaymentSchedule>
    {
        public void Configure(EntityTypeBuilder<PaymentSchedule> builder)
        {
            builder.HasKey(x => x.PaymentScheduleId);

            builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");

            builder.HasOne(x => x.PaymentPlan)
                   .WithMany(p => p.Schedules)
                   .HasForeignKey(x => x.PaymentPlanId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.PaymentPlanId, x.DueDate }).IsUnique();
        }
    }
}
