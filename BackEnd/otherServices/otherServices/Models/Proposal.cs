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

        public double? Offeredprice { get; set; }               // ✅ nullable
        public double? HighestOfferOnPost { get; set; }         // ✅ NEW

        [Required]
        public ProposalStatus ProposalStatus { get; set; } = ProposalStatus.Waiting;

        public IsInstallment IsInstallment { get; set; } = IsInstallment.Cash;

        // SALE installment
        public int? InstallmentDurationMonths { get; set; }
        public int? InstallmentIntervalMonths { get; set; }

        public AIInstallmentDecision IsAble { get; set; } = AIInstallmentDecision.NotCertain;

        [ForeignKey("TenantId")]
        [JsonIgnore]
        public virtual User User { get; set; }

        [ForeignKey("PostId")]
        [JsonIgnore]
        public virtual Post Post { get; set; }

        public double? DownPayment { get; set; }
        public double? InstallmentAmount { get; set; }

        // داخل class Proposal
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    }
}
