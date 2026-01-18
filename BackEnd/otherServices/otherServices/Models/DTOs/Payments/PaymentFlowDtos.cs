using System.ComponentModel.DataAnnotations;
using otherServices.Models.Enums;

namespace otherServices.Models.DTOs.Payments
{
    public class BuyPostRequestDto
    {
        [Required] public long PostId { get; set; }
        [Required] public long PaymentCardId { get; set; }

        [Required, MinLength(3), MaxLength(4)]
        public string CVV { get; set; } = null!;

        [Required, MaxLength(100)]
        public string ExternalRef { get; set; } = null!;

        [Required]
        public IsInstallment IsInstallment { get; set; } = IsInstallment.Cash;

        [Range(1, 360)]
        public int? InstallmentMonths { get; set; }

        public InstallmentFrequency? Frequency { get; set; }
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
