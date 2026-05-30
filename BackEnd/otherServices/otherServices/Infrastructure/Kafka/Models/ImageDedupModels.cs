// Infrastructure/Kafka/Models/ImageDedupModels.cs
// Matches Python kafka_service.py input/output JSON exactly

namespace otherServices.Infrastructure.Kafka.Models;

/// <summary>
/// Sent to Kafka topic: new-listing-images
/// Matches Python kafka_service.py input format exactly.
/// </summary>
public class ImageDedupRequest
{
    public string ImageUrl { get; set; } = default!;
    public string ListingId { get; set; } = default!;   // PostId.ToString()
    public string ImageId { get; set; } = default!;   // PostImage PK as string
    public bool SaveIfNew { get; set; } = true;
}

/// <summary>
/// Received from Kafka topic: image-validation-results
/// Matches Python kafka_service.py output format exactly.
/// </summary>
public class ImageDedupResult
{
    public string ImageId { get; set; } = default!;
    public string ListingId { get; set; } = default!;
    public string? ImageUrl { get; set; }
    public bool ExistsBefore { get; set; }
    public string? MatchedImageId { get; set; }
    public double EmbeddingScore { get; set; }
    public double HashSimilarity { get; set; }
    public int HashDistance { get; set; }
    public string Confidence { get; set; } = default!;
    public string DecisionReason { get; set; } = default!;
    public int TotalIndexed { get; set; }

    // Error case from Python
    public string? Status { get; set; }
    public string? Message { get; set; }
}
