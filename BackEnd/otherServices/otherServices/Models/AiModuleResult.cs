using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace otherServices.Models
{
    /// <summary>
    /// Generic storage for ANY AI module result (all 29 modules).
    /// - We always store the whole payload JSON (PayloadJson) for audit/history.
    /// - We optionally store normalized values (DecisionInt/Score/Reason) for quick querying.
    /// </summary>
    public class AiModuleResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AiModuleResultId { get; set; }

        /// <summary>
        /// Unique RequestId coming from Kafka envelope (AiResultEnvelope.RequestId).
        /// Helps deduplicate results (idempotent).
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string RequestId { get; set; } = default!;

        /// <summary>
        /// The AI module type (AiRequestTypes.*) e.g. "fraud.document_analysis"
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string RequestType { get; set; } = default!;

        /// <summary>
        /// Entity type: "user" | "landlord" | "company" | "post" | "proposal" | "payment_card" | "complaint"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = default!;

        /// <summary>
        /// Entity primary key value (UserId/PostId/ProposalId/...)
        /// </summary>
        [Required]
        public long EntityId { get; set; }

        /// <summary>
        /// Optional: store decision as int (cast to enum as needed: AIDecision, PriceEvaluation, etc.)
        /// </summary>
        public int? DecisionInt { get; set; }

        /// <summary>
        /// Optional: confidence score OR anomaly score OR any numeric score from AI.
        /// </summary>
        public double? Score { get; set; }

        /// <summary>
        /// Optional: human-readable reason/explanation.
        /// </summary>
        [MaxLength(1000)]
        public string? Reason { get; set; }

        /// <summary>
        /// Full raw payload JSON from AI.
        /// This guarantees future-proofing (new modules/fields won't break schema).
        /// </summary>
        [Required]
        public string PayloadJson { get; set; } = "{}";

        /// <summary>
        /// When we stored the row in DB (UTC).
        /// </summary>
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the AI processed it (if provided or when consumer handled it).
        /// </summary>
        public DateTime? ProcessedAtUtc { get; set; }
    }
}
