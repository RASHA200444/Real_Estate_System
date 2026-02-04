using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using otherServices.Infrastructure.Kafka.Models;
using otherServices.Models;

namespace otherServices.Infrastructure.Kafka
{
    /// <summary>
    /// Transactional dispatcher:
    /// - Creates AiRequestEnvelope
    /// - Stores it in AiOutboxMessages
    /// - DOES NOT send Kafka directly
    /// - Send happens in AiOutboxPublisherWorker
    /// </summary>
    public class AiRequestDispatcher : IAiRequestDispatcher
    {
        private readonly AppDbContext2 _db;

        public AiRequestDispatcher(AppDbContext2 db)
        {
            _db = db;
        }

        public Task<string> EnqueueAsync(
            string requestType,
            string entityType,
            long entityId,
            object payload,
            string? requestId = null,
            CancellationToken ct = default)
        {
            var payloadElement = JsonSerializer.SerializeToElement(
                payload,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );

            var envelope = new AiRequestEnvelope
            {
                RequestId = string.IsNullOrWhiteSpace(requestId) ? Guid.NewGuid().ToString("N") : requestId!,
                RequestType = requestType,
                Entity = new AiEntityRef { Type = entityType, Id = entityId },
                CreatedAtUtc = DateTime.UtcNow,
                Payload = payloadElement
            };

            var envelopeJson = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _db.AiOutboxMessages.Add(new AiOutboxMessage
            {
                RequestId = envelope.RequestId,
                RequestType = envelope.RequestType,
                EntityType = envelope.Entity.Type,
                EntityId = envelope.Entity.Id,
                EnvelopeJson = envelopeJson,
                CreatedAtUtc = DateTime.UtcNow
            });

            // Important: caller will SaveChanges in same transaction of their business operation.
            return Task.FromResult(envelope.RequestId);
        }
    }
}
