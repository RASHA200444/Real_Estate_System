namespace otherServices.Infrastructure.Kafka;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "otherServices";

    // Consumer group for ai.results
    public string GroupId { get; set; } = "otherServices.ai.results";

    // ✅ Topics come from appsettings.json
    public KafkaTopicsOptions Topics { get; set; } = new();
}

public class KafkaTopicsOptions
{
    public string AiRequests { get; set; } = "ai.requests";
    public string AiResults { get; set; } = "ai.results";
}
