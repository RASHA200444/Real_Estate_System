// Models/ImageDedupOutboxMessage.cs
// Outbox table for image dedup requests — same pattern as AiOutboxMessage

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace otherServices.Models;

public class ImageDedupOutboxMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ImageDedupOutboxMessageId { get; set; }

    /// <summary>PostImage identifier (string GUID sent to Python as image_id)</summary>
    [Required]
    [MaxLength(100)]
    public string ImageId { get; set; } = default!;

    /// <summary>FK to the parent post</summary>
    [Required]
    public long PostId { get; set; }

    /// <summary>Full serialized ImageDedupRequest JSON</summary>
    [Required]
    public string EnvelopeJson { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }

    public int Attempts { get; set; } = 0;

    [MaxLength(2000)]
    public string? LastError { get; set; }
}
