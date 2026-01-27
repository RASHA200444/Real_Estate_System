namespace otherServices.Services.Payments.Eligibility
{
    public class AiEligibilityResponse
    {
        public bool Success { get; set; }
        public int Score { get; set; } = 0; // 0..100
        public string Reason { get; set; } = "Not evaluated";
        public string Decision { get; set; } = "NotCertain"; // Able / NotCertain / Disable
    }

    public interface IAiEligibilityClient
    {
        Task<AiEligibilityResponse> EvaluateAsync(string jsonPayload, CancellationToken ct = default);
    }
}
