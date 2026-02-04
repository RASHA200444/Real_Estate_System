using System.Threading;
using System.Threading.Tasks;

namespace otherServices.Infrastructure.Kafka
{
    public interface IAiRequestDispatcher
    {
        Task<string> EnqueueAsync(
            string requestType,
            string entityType,
            long entityId,
            object payload,
            string? requestId = null,
            CancellationToken ct = default);
    }
}
