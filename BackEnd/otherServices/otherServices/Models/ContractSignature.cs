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

        [Required]
        [MaxLength(32)]
        public string SignatureAlgo { get; set; } = "ServerHMAC-SHA256";

        // base64 signature
        [Required]
        public string SignatureValue { get; set; } = string.Empty;

        // ✅ NEW: store exact payload used to compute HMAC
        // so verification can recompute signature 1:1
        [Required]
        public string SignedPayload { get; set; } = string.Empty;

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        [ForeignKey(nameof(ContractId))]
        public Contract Contract { get; set; } = null!;
    }
}
