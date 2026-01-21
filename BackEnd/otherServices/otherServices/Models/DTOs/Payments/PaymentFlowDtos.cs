using System.ComponentModel.DataAnnotations;
using otherServices.Models.Enums;

namespace otherServices.Models.DTOs.Payments
{
    public class BuyPostRequestDto
    {
        public long PostId { get; set; }
        public long ProposalId { get; set; }   // ✅ NEW (مهم جدا)

        public long PaymentCardId { get; set; }
        public string CVV { get; set; }
        public string ExternalRef { get; set; }

        public IsInstallment IsInstallment { get; set; } = IsInstallment.Cash;

        public int? InstallmentMonths { get; set; }
        public int? Frequency { get; set; }
    }


    public class AcceptProposalPayRequestDto
    {
        [Required] public long ProposalId { get; set; }
        [Required] public long PaymentCardId { get; set; }

        [Required, MinLength(3), MaxLength(4)]
        public string CVV { get; set; } = null!;

        [Required, MaxLength(100)]
        public string ExternalRef { get; set; } = null!;
    }

    public class PayRemainingRequestDto
    {
        // واحد من الاتنين لازم:
        public long? PaymentPlanId { get; set; }
        public long? PaymentScheduleId { get; set; }

        [Required] public long PaymentCardId { get; set; }
        [Required] public string CVV { get; set; } = null!;
        [Required, MaxLength(100)] public string ExternalRef { get; set; } = null!;
    }
}
