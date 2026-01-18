using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class PaymentPlan
    {
        public long PaymentPlanId { get; set; }

        [Required] public long PostId { get; set; }
        public Post Post { get; set; } = null!;

        // اللي هيدفع (Tenant في الإيجار / Buyer في التقسيط)
        [Required] public long PayerUserId { get; set; }
        public User PayerUser { get; set; } = null!;

        // اللي له الفلوس (LandlordUserId / SellerUserId)
        [Required] public long PayeeUserId { get; set; }
        public User PayeeUser { get; set; } = null!;

        // Rent أو Sale (من PropertyType)
        [Required] public PropertyType PropertyType { get; set; }

        // Cash أو Installment (IsInstallment)
        [Required] public IsInstallment IsInstallment { get; set; }

        // الكارت اللي هيتم السحب منه تلقائيًا
        [Required] public long PaymentCardId { get; set; }
        public PaymentCard PaymentCard { get; set; } = null!;

        // للإيجار: Start/End لازم
        [Column(TypeName = "date")]
        public DateTime? StartDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? EndDate { get; set; }

        // للتقسيط: مدة + interval بالشهور (1/3/6/12)
        public int? DurationMonths { get; set; }
        public int? IntervalMonths { get; set; }

        // المبلغ الكلي (Sale فقط)
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        // مبلغ كل دفعة (Rent: شهري / Sale installment: محسوب)
        [Column(TypeName = "decimal(18,2)")]
        public decimal PeriodicAmount { get; set; } = 0;

        // نسبة الموقع
        [Column(TypeName = "decimal(5,2)")]
        public decimal PlatformFeePercent { get; set; } = 0;

        public PlanStatus Status { get; set; } = PlanStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PaymentSchedule> Schedules { get; set; } = new List<PaymentSchedule>();
    }
}
