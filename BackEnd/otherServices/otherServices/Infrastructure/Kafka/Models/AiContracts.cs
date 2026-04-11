using System.Text.Json;

namespace otherServices.Infrastructure.Kafka.Models;

public class AiEntityRef
{
  
    public string Type { get; set; } = default!;
    public long Id { get; set; }
}

public class AiRequestEnvelope
{
    // Correlation id: AI لازم يرجّعه نفس القيمة في AiResultEnvelope
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");

    // واحد من AiRequestTypes.*
    public string RequestType { get; set; } = default!;

    // مين الـ entity المقصودة
    public AiEntityRef Entity { get; set; } = new();

    // للتتبع
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Payload: بيانات الطلب اللي AI محتاجها (نص، paths، metadata..)
    public JsonElement Payload { get; set; }
}

public class AiResultEnvelope
{
    // لازم يطابق AiRequestEnvelope.RequestId
    public string RequestId { get; set; } = default!;

    // لازم يطابق AiRequestEnvelope.RequestType
    public string RequestType { get; set; } = default!;

    public AiEntityRef Entity { get; set; } = new();

    // Payload: نتيجة AI (decision/score/reason/labels... إلخ)
    public JsonElement Payload { get; set; }

    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
