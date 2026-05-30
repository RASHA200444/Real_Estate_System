// Infrastructure/Kafka/ImageDedupOutboxPublisherWorker.cs
// Polls ImageDedupOutboxMessages and publishes to new-listing-images topic.
// Mirrors AiOutboxPublisherWorker pattern exactly.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using otherServices.Models;

namespace otherServices.Infrastructure.Kafka;

public class ImageDedupOutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaProducer _producer;
    private readonly KafkaOptions _options;
    private readonly ILogger<ImageDedupOutboxPublisherWorker> _logger;

    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public ImageDedupOutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        IKafkaProducer producer,
        IOptions<KafkaOptions> options,
        ILogger<ImageDedupOutboxPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _producer = producer;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ImageDedupOutboxPublisherWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImageDedupOutboxPublisherWorker loop.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext2>();

        // Grab up to 50 unsent messages ordered by creation time
        var pending = await db.ImageDedupOutboxMessages
            .Where(m => m.SentAtUtc == null && m.Attempts < 5)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        if (!pending.Any()) return;

        foreach (var msg in pending)
        {
            try
            {
                await _producer.ProduceAsync(
                    _options.Topics.ImageDedupRequests,
                    msg.ImageId,            // Kafka message key
                    msg.EnvelopeJson,
                    ct
                );

                msg.SentAtUtc = DateTime.UtcNow;
                msg.Attempts++;
                _logger.LogInformation(
                    "ImageDedup published: imageId={ImageId} postId={PostId}",
                    msg.ImageId, msg.PostId);
            }
            catch (Exception ex)
            {
                msg.Attempts++;
                msg.LastError = ex.Message;
                _logger.LogWarning(
                    "Failed to publish imageId={ImageId}: {Error}",
                    msg.ImageId, ex.Message);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
