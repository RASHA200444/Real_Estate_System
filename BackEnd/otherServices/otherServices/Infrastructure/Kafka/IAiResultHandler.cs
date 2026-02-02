using otherServices.Infrastructure.Kafka.Models;

namespace otherServices.Infrastructure.Kafka;

public interface IAiResultHandler
{
    Task HandleAsync(AiResultEnvelope envelope, CancellationToken ct = default);
}
