using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using otherServices.Infrastructure.Kafka.Models;

namespace otherServices.Infrastructure.Kafka;

/// <summary>
/// Background worker:
/// - subscribes to ai.results
/// - deserializes AiResultEnvelope
/// - delegates to IAiResultHandler
/// </summary>
public class AiResultsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;

    public AiResultsConsumer(IServiceScopeFactory scopeFactory, IOptions<KafkaOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,

            // Manual commit to avoid losing messages on crashes
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        // ✅ topic comes from appsettings.json (Kafka:Topics:AiResults)
        consumer.Subscribe(_options.Topics.AiResults);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(ct);
                if (cr?.Message?.Value == null) continue;

                var envelope = JsonSerializer.Deserialize<AiResultEnvelope>(
                    cr.Message.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (envelope != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IAiResultHandler>();

                    handler.HandleAsync(envelope, ct).GetAwaiter().GetResult();
                }

                consumer.Commit(cr);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // TODO: add ILogger and log exception
                // Important: DO NOT crash the worker on a single message.
            }
        }

        consumer.Close();
    }
}
