using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class ContractSignature
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ContractSignatureId { get; set; }

        [Required]
        public long ContractId { get; set; }

        [Required]
        public long SignerUserId { get; set; }

        [Required]
        public SignerRole SignerRole { get; set; }

        [Required]
        public DateTime SignedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(64)]
        public string ContractHash { get; set; } = string.Empty;

        // "Ed25519" (recommended)
        [Required]
        [MaxLength(32)]
        public string SignatureAlgo { get; set; } = "Ed25519";

        // base64 signature
        [Required]
        public string SignatureValue { get; set; } = string.Empty;

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        [ForeignKey(nameof(ContractId))]
        public Contract Contract { get; set; } = null!;
    }
}
