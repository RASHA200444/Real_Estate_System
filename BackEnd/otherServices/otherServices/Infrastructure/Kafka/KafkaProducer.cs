using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace otherServices.Infrastructure.Kafka;

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

    public Task ProduceAsync(string topic, string message, CancellationToken ct = default)
    {
        // keep old behavior but route to new one
        return ProduceAsync(topic, Guid.NewGuid().ToString("N"), message, ct);
    }

    public async Task ProduceAsync(string topic, string key, string message, CancellationToken ct = default)
    {
        var msg = new Message<string, string>
        {
            Key = key,
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
