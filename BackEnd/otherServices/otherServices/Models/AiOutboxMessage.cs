using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace otherServices.Models
{
    public class AiOutboxMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AiOutboxMessageId { get; set; }

        [Required]
        [MaxLength(64)]
        public string RequestId { get; set; } = default!;

        [Required]
        [MaxLength(200)]
        public string RequestType { get; set; } = default!;

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = default!;

        [Required]
        public long EntityId { get; set; }

        [Required]
        public string EnvelopeJson { get; set; } = default!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? SentAtUtc { get; set; }

        public int Attempts { get; set; } = 0;

        [MaxLength(2000)]
        public string? LastError { get; set; }
    }
}
