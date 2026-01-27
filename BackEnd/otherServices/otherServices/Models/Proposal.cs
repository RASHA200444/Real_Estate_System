using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class Proposal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ProposalId { get; set; }

        [Required]
        public long PostId { get; set; }

        [Required]
        public long TenantId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Phone { get; set; }

        [Column(TypeName = "date")]
        public DateTime? StartRentalDate { get; set; }

        [Column(TypeName = "date")]
        public DateTime? EndRentalDate { get; set; }

        [Required]
        [MaxLength(255)]
        public string FilePath { get; set; }

        public double? Offeredprice { get; set; }
        public double? HighestOfferOnPost { get; set; }

        [Required]
        public ProposalStatus ProposalStatus { get; set; } = ProposalStatus.Waiting;

        public IsInstallment IsInstallment { get; set; } = IsInstallment.Cash;

        // SALE installment
        public int? InstallmentDurationMonths { get; set; }
        public int? InstallmentIntervalMonths { get; set; }

        // ✅ Existing AI decision for installment
        public AIInstallmentDecision IsAble { get; set; } = AIInstallmentDecision.NotCertain;

        // ✅ NEW: AI decision for rent (eligibility)
        public AIRentDecision RentIsAble { get; set; } = AIRentDecision.NotCertain;

        // ✅ Store eligibility answers as JSON text (sent with proposal)
        [Column(TypeName = "nvarchar(max)")]
        public string? EligibilityAnswersJson { get; set; }

        // optional AI outputs later
        public int? EligibilityScore { get; set; }

        [MaxLength(500)]
        public string? EligibilityReason { get; set; }

        public DateTime? EligibilityAssessedAt { get; set; }

        [ForeignKey("TenantId")]
        [JsonIgnore]
        public virtual User User { get; set; }

        [ForeignKey("PostId")]
        [JsonIgnore]
        public virtual Post Post { get; set; }

        public double? DownPayment { get; set; }
        public double? InstallmentAmount { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
