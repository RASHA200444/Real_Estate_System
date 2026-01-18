using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
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

        [Required]
        public double Offeredprice { get; set; }

        [Required]
        public ProposalStatus ProposalStatus { get; set; } = ProposalStatus.Waiting;
        public IsInstallment IsInstallment { get; set; } = IsInstallment.Cash; // Cash , Installment


        // ✅ NEW - for SALE installment only (duration & interval in months)
        public int? InstallmentDurationMonths { get; set; }   // مثال 24 شهر
        public int? InstallmentIntervalMonths { get; set; }   // 1 / 3 / 6 / 12


        public AIInstallmentDecision IsAble { get; set; } = AIInstallmentDecision.NotCertain; // Disable = -1 , NotCertain = 0 , Able = 1 

        [ForeignKey("TenantId")]
        [JsonIgnore]
        public virtual User User { get; set; }

        [ForeignKey("PostId")]
        [JsonIgnore]
        public virtual Post Post { get; set; }


        public double? DownPayment { get; set; }            // new
        public double? InstallmentAmount { get; set; }      // new (optional, if derived you can remove later)

    }
}
