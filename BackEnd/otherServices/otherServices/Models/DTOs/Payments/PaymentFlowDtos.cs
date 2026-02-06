using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Payments
{
    // ✅ 1) Sale Cash (INITIATE ONLY) - NO CVV HERE
    public class SaleCashRequestDto
    {
        [Required] public long PostId { get; set; }
        [Required] public long ProposalId { get; set; }

        [Required] public long PaymentCardId { get; set; }

        [Required, MaxLength(100)]
        public string ExternalRef { get; set; } = null!;
    }

    // ✅ 2) Sale Installment (INITIATE ONLY) - NO CVV HERE
    public class SaleInstallmentRequestDto
    {
        [Required] public long PostId { get; set; }
        [Required] public long ProposalId { get; set; }

        [Required] public long PaymentCardId { get; set; }

        [Required, MaxLength(100)]
        public string ExternalRef { get; set; } = null!;

        [Required, Range(1, 360)]
        public int InstallmentMonths { get; set; }

        // 1/3/6/12 ... بالأشهر
        [Required, Range(1, 12)]
        public int Frequency { get; set; }
    }

    // ✅ 3) Rent Start (INITIATE ONLY) - NO CVV HERE
    public class RentStartPaymentRequestDto
    {
        [Required] public long ProposalId { get; set; }

        [Required] public long PaymentCardId { get; set; }

        [Required, MaxLength(100)]
        public string ExternalRef { get; set; } = null!;
    }

    // ✅ 4) Pay Remaining (PAYS NOW) - CVV REQUIRED
    public class PayRemainingRequestDto
    {
        // واحد من الاتنين لازم:
        public long? PaymentPlanId { get; set; }
        public long? PaymentScheduleId { get; set; }

        [Required] public long PaymentCardId { get; set; }

        [Required, MinLength(3), MaxLength(4)]
        public string CVV { get; set; } = null!;

        [Required, MaxLength(100)]
        public string ExternalRef { get; set; } = null!;
    }
}
