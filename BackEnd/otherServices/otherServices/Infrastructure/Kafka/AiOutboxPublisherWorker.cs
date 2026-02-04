using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using otherServices.Models;

namespace otherServices.Infrastructure.Kafka
{
    /// <summary>
    /// Reads unsent messages from AiOutboxMessages and publishes to Kafka Topics.AiRequests.
    /// </summary>
    public class AiOutboxPublisherWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly KafkaOptions _kafka;

        public AiOutboxPublisherWorker(IServiceScopeFactory scopeFactory, IOptions<KafkaOptions> kafkaOptions)
        {
            _scopeFactory = scopeFactory;
            _kafka = kafkaOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext2>();
                    var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

                    var batch = await db.AiOutboxMessages
                        .Where(x => x.SentAtUtc == null && x.Attempts < 20)
                        .OrderBy(x => x.AiOutboxMessageId)
                        .Take(50)
                        .ToListAsync(stoppingToken);

                    foreach (var msg in batch)
                    {
                        try
                        {
                            await producer.ProduceAsync(_kafka.Topics.AiRequests, msg.RequestId, msg.EnvelopeJson, stoppingToken);

                            msg.SentAtUtc = DateTime.UtcNow;
                            msg.LastError = null;
                        }
                        catch (Exception ex)
                        {
                            msg.Attempts++;
                            msg.LastError = ex.Message;
                        }
                    }

                    if (batch.Count > 0)
                        await db.SaveChangesAsync(stoppingToken);
                }
                catch
                {
                    // keep silent to avoid crashing worker; you can add ILogger later
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
