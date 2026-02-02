namespace otherServices.Infrastructure.Kafka;

/// <summary>
/// Kafka settings loaded from appsettings.json section "Kafka"
/// </summary>
public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "otherServices";

    // Consumer group for ai.results
    public string GroupId { get; set; } = "otherServices.ai.results";
}
