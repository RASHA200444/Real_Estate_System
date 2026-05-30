// Infrastructure/Kafka/KafkaOptions.cs
namespace otherServices.Infrastructure.Kafka;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "otherServices";
    public string GroupId { get; set; } = "otherServices.ai.results";

    public KafkaTopicsOptions Topics { get; set; } = new();
}

public class KafkaTopicsOptions
{
    // ── Existing AI service topics ───────────────────────────────
    public string AiRequests { get; set; } = "ai.requests";
    public string AiResults { get; set; } = "ai.results";

    // ── Image dedup service topics (separate Python service) ─────
    public string ImageDedupRequests { get; set; } = "new-listing-images";
    public string ImageDedupResults { get; set; } = "image-validation-results";
}
