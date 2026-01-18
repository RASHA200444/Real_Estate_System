using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class Contract
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ContractId { get; set; }

        public long? ProposalId { get; set; }
        public long PostId { get; set; }

        public long TenantId { get; set; }          // Buyer / Renter
        public long LandlordUserId { get; set; }    // Seller / Lessor (UserId)

        public ContractType Type { get; set; }
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        // Canonical snapshot that was signed
        [Required]
        public string ContractJson { get; set; } = string.Empty;

        // SHA256 of canonical json (hex or base64)
        [Required]
        [MaxLength(64)]
        public string ContractHash { get; set; } = string.Empty;

        public int Version { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ContractSignature> Signatures { get; set; } = new List<ContractSignature>();
    }
}
