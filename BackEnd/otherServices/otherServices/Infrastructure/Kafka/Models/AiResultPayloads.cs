namespace otherServices.Infrastructure.Kafka.Models;

/// <summary>
/// Generic decision result: AI بيرجع Decision كـ int
/// وإحنا في handler بنحوّله للـ enum المناسب.
/// </summary>
public class AiDecisionResult
{
    public int Decision { get; set; }        // will be mapped to enum in handler
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public class PriceEvaluationResult
{
    public int PriceEvaluation { get; set; } // mapped to PriceEvaluation enum
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public class EligibilityResult
{
    public int IsAble { get; set; }          // mapped to AIInstallmentDecision / AIRentDecision
    public int? Score { get; set; }
    public string? Reason { get; set; }
}

public class ContentModerationResult
{
    public bool IsAllowed { get; set; }
    public string? Reason { get; set; }
    public int? Severity { get; set; }
}

public class AnomalyResult
{
    public bool IsSuspicious { get; set; }
    public double Score { get; set; }
    public string? Reason { get; set; }
}

// ===============================
// OPTIONAL: payloads for NEW modules
// (مش لازم تستخدمهم دلوقتي، بس جاهزين)
// ===============================

public class TextAnalysisResult
{
    public double Score { get; set; }     // sentiment/toxicity/spam score
    public string? Label { get; set; }    // "positive"/"negative"/"toxic"/...
    public string? Reason { get; set; }
}

public class SearchRankingResult
{
    public List<long> RankedEntityIds { get; set; } = new(); // e.g., post ids
    public string? Reason { get; set; }
}

public class RecommendationResult
{
    public List<long> RecommendedEntityIds { get; set; } = new();
    public string? Reason { get; set; }
}

public class NegotiationSuggestionResult
{
    public decimal? SuggestedPrice { get; set; }
    public string? SuggestedMessage { get; set; }
    public string? Reason { get; set; }
}
