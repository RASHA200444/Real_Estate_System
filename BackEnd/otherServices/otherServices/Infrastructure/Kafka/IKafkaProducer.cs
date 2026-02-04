namespace otherServices.Infrastructure.Kafka
{
    public interface IKafkaProducer
    {
        Task ProduceAsync(string topic, string message, CancellationToken ct = default);

        // ✅ NEW: deterministic key (RequestId)
        Task ProduceAsync(string topic, string key, string message, CancellationToken ct = default);
    }
}
