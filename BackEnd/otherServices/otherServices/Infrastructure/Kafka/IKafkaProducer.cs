namespace otherServices.Infrastructure.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync(string topic, string message, CancellationToken ct = default);
}
