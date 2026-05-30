
// Infrastructure/Kafka/ImageDedupResultsConsumer.cs
// Subscribes to image-validation-results topic.
// For each result:
//   - ExistsBefore = true  → post.ImageManipulationEvaluation = Fraudulent
//   - ExistsBefore = false → post.ImageManipulationEvaluation = Verified
// After ALL images of a post are processed → re-runs FinalizePostStatus.

using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using otherServices.Infrastructure.Kafka.Models;
using otherServices.Models;
using otherServices.Models.Enums;

namespace otherServices.Infrastructure.Kafka;

public class ImageDedupResultsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<ImageDedupResultsConsumer> _logger;

    public ImageDedupResultsConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<ImageDedupResultsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    // ── Main loop ────────────────────────────────────────────────────

    private void ConsumeLoop(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = $"{_options.GroupId}.image-dedup",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_options.Topics.ImageDedupResults);

        _logger.LogInformation(
            "ImageDedupResultsConsumer listening on: {Topic}",
            _options.Topics.ImageDedupResults);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(ct);
                if (cr?.Message?.Value == null) continue;

                var result = JsonSerializer.Deserialize<ImageDedupResult>(
                    cr.Message.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (result != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext2>();
                    HandleResultAsync(result, db, ct).GetAwaiter().GetResult();
                }

                consumer.Commit(cr);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image-dedup result.");
                // Never crash the loop — mirrors AiResultsConsumer pattern
            }
        }

        consumer.Close();
    }

    // ── Handler ──────────────────────────────────────────────────────

    private async Task HandleResultAsync(
        ImageDedupResult result,
        AppDbContext2 db,
        CancellationToken ct)
    {
        // Parse postId from ListingId (we set it as PostId.ToString())
        if (!long.TryParse(result.ListingId, out var postId))
        {
            _logger.LogWarning("ImageDedupResult has invalid ListingId: {Id}", result.ListingId);
            return;
        }

        // ── Error from Python side ────────────────────────────────
        if (result.Status == "error")
        {
            _logger.LogWarning(
                "ImageDedup error for imageId={ImageId} postId={PostId}: {Msg}",
                result.ImageId, postId, result.Message);
            return;
        }

        // ── Load post ─────────────────────────────────────────────
        var post = await db.Posts
            .Include(p => p.PostImages)
            .FirstOrDefaultAsync(p => p.PostId == postId, ct);

        if (post == null)
        {
            _logger.LogWarning("Post not found for imageId={ImageId}", result.ImageId);
            return;
        }

        // ── Apply decision ────────────────────────────────────────
        // ExistsBefore = true  → this image was seen before = Fraudulent
        // ExistsBefore = false → brand new image = Verified
        var decision = result.ExistsBefore
            ? AIDecision.Fraudulent
            : AIDecision.Verified;

        // If ANY image is flagged as duplicate → mark the whole post
        // (keep Fraudulent if already set, don't downgrade)
        if (post.ImageManipulationEvaluation != AIDecision.Fraudulent)
        {
            post.ImageManipulationEvaluation = decision;
        }

        post.AiLastCheckedAt = DateTime.UtcNow;

        if (result.ExistsBefore)
        {
            post.AiReason = $"Duplicate image detected. " +
                            $"Matched: {result.MatchedImageId}, " +
                            $"Score: {result.EmbeddingScore:F2}, " +
                            $"HashDist: {result.HashDistance}";
            post.AiConfidence = result.EmbeddingScore;

            _logger.LogWarning(
                "Duplicate image detected! postId={PostId} imageId={ImageId} matchedId={MatchedId}",
                postId, result.ImageId, result.MatchedImageId);
        }
        else
        {
            _logger.LogInformation(
                "Image OK: postId={PostId} imageId={ImageId} score={Score:F2}",
                postId, result.ImageId, result.EmbeddingScore);
        }

        // ── Check if ALL images of this post have been deduped ────
        var totalImages = post.PostImages?.Count ?? 0;

        // Count how many outbox messages for this post are now sent
        var sentCount = await db.ImageDedupOutboxMessages
            .CountAsync(m => m.PostId == postId && m.SentAtUtc != null, ct);

        await db.SaveChangesAsync(ct);

        // ── Re-run finalize only when all images are done ─────────
        if (totalImages > 0 && sentCount >= totalImages)
        {
            _logger.LogInformation(
                "All {Count} images deduped for postId={PostId}. Running finalize.",
                totalImages, postId);

            // Delegate to AiResultHandler's finalize logic via a scoped call
            var handler = _scopeFactory.CreateScope()
                              .ServiceProvider
                              .GetRequiredService<IAiResultHandler>();

            // We trigger finalize by sending a synthetic "image manipulation" result
            // through the existing handler so all the existing finalize logic runs
            var syntheticEnvelope = new AiResultEnvelope
            {
                RequestId = Guid.NewGuid().ToString("N"),
                RequestType = AiRequestTypes.Fraud_ImageManipulation,
                Entity = new AiEntityRef { Type = "post", Id = postId },
                Payload = JsonSerializer.SerializeToElement(new
                {
                    decision = (int)post.ImageManipulationEvaluation!,
                    confidence = result.EmbeddingScore,
                    reason = post.AiReason ?? "Image dedup complete"
                })
            };

            await handler.HandleAsync(syntheticEnvelope, ct);
        }
    }
}
