using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace otherServices.Models
{
    public class PaymentSchedule
    {
        public long PaymentScheduleId { get; set; }

        [Required] public long PaymentPlanId { get; set; }
        public PaymentPlan PaymentPlan { get; set; } = null!;

        [Required, Column(TypeName = "date")]
        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public bool IsPaid { get; set; } = false;
        public DateTime? PaidAt { get; set; }

        // آخر Transaction نجح للسطر ده
        public long? TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        // فشل/محاولات
        public int FailedAttempts { get; set; } = 0;
        public DateTime? LastFailureAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public bool EscalatedToAdmin { get; set; } = false;

        [MaxLength(500)]
        public string? LastError { get; set; }
    }
}
