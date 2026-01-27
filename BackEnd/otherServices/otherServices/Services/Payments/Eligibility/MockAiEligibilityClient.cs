using System.Text.Json;

namespace otherServices.Services.Payments.Eligibility
{
    // ✅ For now: purely rule-based local evaluation (no Kafka, no AI)
    public class MockAiEligibilityClient : IAiEligibilityClient
    {
        public Task<AiEligibilityResponse> EvaluateAsync(string jsonPayload, CancellationToken ct = default)
        {
            // We DO NOT “do AI” here — just return a placeholder so later you can swap implementation.
            // Real logic is in EligibilityService using rules + requiredPayment.
            _ = JsonDocument.Parse(jsonPayload); // validate json doesn't crash
            return Task.FromResult(new AiEligibilityResponse
            {
                Success = true,
                Score = 50,
                Reason = "Mock evaluation (AI placeholder).",
                Decision = "NotCertain"
            });
        }
    }
}
