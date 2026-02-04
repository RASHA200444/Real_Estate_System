namespace otherServices.Infrastructure.Kafka.Models;

/// <summary>
/// ===============================
/// AI Result Payloads (29 modules)
/// ===============================
/// كل RequestType له Result Payload خاص.
/// ملاحظة:
/// - أغلب الـ payloads فيها Confidence + Reason.
/// - بعضهم Decision (int) يتعمله mapping لـ enum في handler.
/// - بعضهم Score أو Lists أو Text.
/// </summary>

#region Shared / Base Contracts

public interface IAiResultPayload
{
    double Confidence { get; set; }
    string? Reason { get; set; }
}

/// <summary>
/// Result يحتوي قرار Decision كـ int.
/// الهاندلر بيحوّله للـ enum المناسب.
/// </summary>
public abstract class AiDecisionPayloadBase : IAiResultPayload
{
    public int Decision { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

#endregion

// =========================================================================
// 01) Fraud / Documents (3)
// =========================================================================

public sealed class FraudDocumentAnalysisResult : AiDecisionPayloadBase
{
    // Decision => AIDecision (Uncertain/Verified/Fraudulent/NotReviewed)
}

public sealed class FraudOwnershipDocumentAnalysisResult : AiDecisionPayloadBase
{
    // Decision => AIDecision
}

public sealed class FraudCommercialRegisterAnalysisResult : AiDecisionPayloadBase
{
    // Decision => AIDecision
}

// =========================================================================
// 02) Fraud / Posts (3)
// =========================================================================

public sealed class FraudFakePropertyDetectionResult : AiDecisionPayloadBase
{
    // Decision => AIDecision
    // You may add duplicateRefId / similarity score later.
}

public sealed class FraudImageManipulationResult : AiDecisionPayloadBase
{
    // Decision => AIDecision
    // You may add forgeryFlags later.
}

public sealed class PriceAnomalyDetectionResult : IAiResultPayload
{
    // PriceEvaluation => PriceEvaluation enum (-2..2)
    public int PriceEvaluation { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 03) Buyer / Proposals (2)
// =========================================================================

public sealed class BuyerInstallmentRiskResult : IAiResultPayload
{
    // IsAble => AIInstallmentDecision
    public int IsAble { get; set; }
    public int? Score { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class BuyerRentEligibilityResult : IAiResultPayload
{
    // IsAble => AIRentDecision
    public int IsAble { get; set; }
    public int? Score { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 04) Payment (1)
// =========================================================================

public sealed class PaymentFraudDetectionResult : IAiResultPayload
{
    // Decision => AIDecision (Fraudulent => disable card)
    public int Decision { get; set; }
    public int? Score { get; set; } // optional numeric fraud score
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 05) Content / Reports / Anomaly (3)
// =========================================================================

public sealed class ContentModerationResult : IAiResultPayload
{
    public bool IsAllowed { get; set; }
    public int? Severity { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class ReportsSmartAnalysisResult : IAiResultPayload
{
    // Decision for complaint/report seriousness, etc.
    public int Decision { get; set; }
    public int? Severity { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class UserAnomalyDetectionResult : IAiResultPayload
{
    public bool IsSuspicious { get; set; }
    public double Score { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 06) Content / Text (4)
// =========================================================================

public sealed class ContentSpamDetectionResult : IAiResultPayload
{
    public bool IsSpam { get; set; }
    public double Score { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class ContentToxicityScoringResult : IAiResultPayload
{
    public double ToxicityScore { get; set; }
    public int? Severity { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class ContentSentimentAnalysisResult : IAiResultPayload
{
    public string? Label { get; set; } // positive/neutral/negative
    public double Score { get; set; }  // sentiment strength
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class ContentLanguageDetectionResult : IAiResultPayload
{
    public string? LanguageCode { get; set; } // "ar", "en", ...
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 07) Search / Retrieval (3)
// =========================================================================

public sealed class SearchQueryUnderstandingResult : IAiResultPayload
{
    public string? Intent { get; set; } // "rent", "buy", ...
    public string? EntitiesJson { get; set; } // optional extracted filters JSON
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class SearchSemanticRankingResult : IAiResultPayload
{
    public List<long> RankedPostIds { get; set; } = new();
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class SearchSimilarListingsResult : IAiResultPayload
{
    public List<long> SimilarPostIds { get; set; } = new();
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 08) Recommendation / Personalization (3)
// =========================================================================

public sealed class RecoPersonalizedFeedResult : IAiResultPayload
{
    public List<long> RecommendedPostIds { get; set; } = new();
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class RecoRelatedPostsResult : IAiResultPayload
{
    public List<long> RelatedPostIds { get; set; } = new();
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class RecoUserToUserMatchResult : IAiResultPayload
{
    public List<long> MatchedUserIds { get; set; } = new();
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 09) Negotiation / Pricing (2)
// =========================================================================

public sealed class NegotiationPriceSuggestionResult : IAiResultPayload
{
    public decimal? SuggestedPrice { get; set; }
    public string? SuggestedMessage { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class NegotiationCounterOfferSuggestionResult : IAiResultPayload
{
    public decimal? CounterOfferPrice { get; set; }
    public string? CounterOfferMessage { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 10) Insights / Analytics (3)
// =========================================================================

public sealed class InsightsMarketTrendsResult : IAiResultPayload
{
    public string? ReportJson { get; set; } // aggregated JSON
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class InsightsDemandPredictionResult : IAiResultPayload
{
    public string? DemandJson { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class InsightsUserBehaviorSummaryResult : IAiResultPayload
{
    public string? SummaryJson { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 11) Contracts / Legal Assist (2)
// =========================================================================

public sealed class ContractRiskFlagsResult : IAiResultPayload
{
    public string? RiskFlagsJson { get; set; }
    public int? Severity { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class ContractClauseSuggestionResult : IAiResultPayload
{
    public string? SuggestedClausesJson { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 12) Support / Operations (3)
// =========================================================================

public sealed class SupportAutoReplySuggestionResult : IAiResultPayload
{
    public string? SuggestedReply { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class SupportTicketClassificationResult : IAiResultPayload
{
    public string? Label { get; set; } // "fraud", "spam", "billing", ...
    public int? Severity { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class SupportPriorityScoringResult : IAiResultPayload
{
    public int Priority { get; set; } // 1..5
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}
