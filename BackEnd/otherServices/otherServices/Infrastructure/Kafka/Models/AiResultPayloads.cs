namespace otherServices.Infrastructure.Kafka.Models;

/// <summary>
/// ===============================
/// AI Result Payloads
/// ===============================
/// كل RequestType له Result Payload خاص.
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

public sealed class FraudDocumentAnalysisResult : AiDecisionPayloadBase { }
public sealed class FraudOwnershipDocumentAnalysisResult : AiDecisionPayloadBase { }
public sealed class FraudCommercialRegisterAnalysisResult : AiDecisionPayloadBase { }

// =========================================================================
// 02) Fraud / Posts (4) ✅
// =========================================================================

public sealed class FraudFakePropertyDetectionResult : AiDecisionPayloadBase { }
public sealed class FraudImageManipulationResult : AiDecisionPayloadBase { }

/// <summary>
/// ✅ NEW: تحليل مستند البوست (Posts.PostDocPath)
/// Decision => AIDecision
/// </summary>
public sealed class FraudPostDocumentAnalysisResult : AiDecisionPayloadBase { }

public sealed class PriceAnomalyDetectionResult : IAiResultPayload
{
    public int PriceEvaluation { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 03) Buyer / Proposals (2)
// =========================================================================

public sealed class BuyerInstallmentRiskResult : IAiResultPayload
{
    public int IsAble { get; set; }
    public int? Score { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class BuyerRentEligibilityResult : IAiResultPayload
{
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
    public int Decision { get; set; }
    public int? Score { get; set; }
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
    public string? Label { get; set; }
    public double Score { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class ContentLanguageDetectionResult : IAiResultPayload
{
    public string? LanguageCode { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 07) Search / Retrieval (3)
// =========================================================================

public sealed class SearchQueryUnderstandingResult : IAiResultPayload
{
    public string? Intent { get; set; }
    public string? EntitiesJson { get; set; }
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
    public string? ReportJson { get; set; }
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
    public string? Label { get; set; }
    public int? Severity { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class SupportPriorityScoringResult : IAiResultPayload
{
    public int Priority { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// Projects doc analysis ✅
// =========================================================================
public sealed class FraudProjectDocumentAnalysisResult : AiDecisionPayloadBase { }

// =========================================================================
// 13) Offers / Auctions (NEW)
// =========================================================================
public sealed class BuyerOfferRankingResult : IAiResultPayload
{
    public long PostId { get; set; }
    public List<long> RankedProposalIds { get; set; } = new();
    public string? RankedJson { get; set; } // optional
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 14) Image Quality (NEW)
// =========================================================================
public sealed class ImageQualityScoringResult : IAiResultPayload
{
    public long PostId { get; set; }
    public double OverallScore { get; set; } // 0..1
    public string? PerImageScoresJson { get; set; } // optional json
    public string? IssuesJson { get; set; } // optional
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 15) Owner Forecasts (NEW)
// =========================================================================
public sealed class OwnerForecastPriceResult : IAiResultPayload
{
    public long PostId { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public string? PriceRangeJson { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class OwnerForecastDemandResult : IAiResultPayload
{
    public long PostId { get; set; }
    public string? DemandLevel { get; set; } // Low/Medium/High
    public string? DemandJson { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

public sealed class OwnerForecastRevenueResult : IAiResultPayload
{
    public long PostId { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public string? RevenueJson { get; set; }
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}

// =========================================================================
// 16) Decision Engine (NEW)
// =========================================================================
public sealed class DecisionEngineResult : IAiResultPayload
{
    // "Accepted" | "Refused" | "Pending" (matches PostPendingStatus names)
    public string? SuggestedPendingStatus { get; set; }
    public int? RiskLevel { get; set; } // optional 0..100
    public double Confidence { get; set; }
    public string? Reason { get; set; }
}
