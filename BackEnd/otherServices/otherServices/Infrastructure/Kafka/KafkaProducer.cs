using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace otherServices.Infrastructure.Kafka;

/// <summary>
/// Simple Kafka producer wrapper.
/// - Used for sending AiRequestEnvelope to ai.requests (and any future topics).
/// </summary>
public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options)
    {
        var opt = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = opt.BootstrapServers,
            ClientId = opt.ClientId,

            // Reliability settings
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, string message, CancellationToken ct = default)
    {
        // key helps partition stability (optional)
        var msg = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString("N"),
            Value = message
        };

        await _producer.ProduceAsync(topic, msg, ct);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(2));
        _producer.Dispose();
    }
}
