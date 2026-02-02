using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using otherServices.Infrastructure.Kafka.Models;
using otherServices.Models;
using otherServices.Models.Enums;

namespace otherServices.Infrastructure.Kafka;

public class AiResultHandler : IAiResultHandler
{
    private readonly AppDbContext2 _db;

    public AiResultHandler(AppDbContext2 db)
    {
        _db = db;
    }

    public async Task HandleAsync(AiResultEnvelope envelope, CancellationToken ct = default)
    {
        if (envelope == null) return;

        switch (envelope.RequestType)
        {
            // =========================================================================
            // 01) Documents (existing DB mapping)
            // =========================================================================
            case AiRequestTypes.Fraud_DocumentAnalysis:
                await HandleUserNidResult(envelope, ct);
                break;

            case AiRequestTypes.Fraud_OwnershipDocumentAnalysis:
                await HandleLandlordOwnershipResult(envelope, ct);
                break;

            case AiRequestTypes.Fraud_CommercialRegisterAnalysis:
                await HandleCompanyRegisterResult(envelope, ct);
                break;

            // =========================================================================
            // 02) Posts (existing DB mapping)
            // =========================================================================
            case AiRequestTypes.Fraud_FakePropertyDetection:
                await HandlePostFraudResult(envelope, ct);
                break;

            case AiRequestTypes.Fraud_ImageManipulation:
                await HandlePostImageManipulationResult(envelope, ct);
                break;

            case AiRequestTypes.Price_AnomalyDetection:
                await HandlePostPriceResult(envelope, ct);
                break;

            // =========================================================================
            // 03) Proposals (existing DB mapping)
            // =========================================================================
            case AiRequestTypes.Buyer_InstallmentRisk:
                await HandleInstallmentEligibilityResult(envelope, ct);
                break;

            case AiRequestTypes.Buyer_RentEligibility:
                await HandleRentEligibilityResult(envelope, ct);
                break;

            // =========================================================================
            // 04) Payment (existing DB mapping)
            // =========================================================================
            case AiRequestTypes.Payment_FraudDetection:
                await HandlePaymentFraudResult(envelope, ct);
                break;

            // =========================================================================
            // 05) Content/Reports/Anomaly (existing DB mapping)
            // =========================================================================
            case AiRequestTypes.Content_Moderation:
                await HandleContentModerationResult(envelope, ct);
                break;

            case AiRequestTypes.Smart_ReportsAnalysis:
                await HandleSmartReportResult(envelope, ct);
                break;

            case AiRequestTypes.User_AnomalyDetection:
                await HandleUserAnomalyResult(envelope, ct);
                break;

            // =========================================================================
            // 06) Content / Text (STUBS - you will map later)
            // =========================================================================
            case AiRequestTypes.Content_SpamDetection:
                await HandleContentSpamDetectionStub(envelope, ct);
                break;

            case AiRequestTypes.Content_ToxicityScoring:
                await HandleContentToxicityStub(envelope, ct);
                break;

            case AiRequestTypes.Content_SentimentAnalysis:
                await HandleContentSentimentStub(envelope, ct);
                break;

            case AiRequestTypes.Content_LanguageDetection:
                await HandleContentLanguageStub(envelope, ct);
                break;

            // =========================================================================
            // 07) Search / Retrieval (STUBS)
            // =========================================================================
            case AiRequestTypes.Search_QueryUnderstanding:
                await HandleSearchQueryUnderstandingStub(envelope, ct);
                break;

            case AiRequestTypes.Search_SemanticRanking:
                await HandleSearchSemanticRankingStub(envelope, ct);
                break;

            case AiRequestTypes.Search_SimilarListings:
                await HandleSearchSimilarListingsStub(envelope, ct);
                break;

            // =========================================================================
            // 08) Recommendation / Personalization (STUBS)
            // =========================================================================
            case AiRequestTypes.Reco_PersonalizedFeed:
                await HandleRecoPersonalizedFeedStub(envelope, ct);
                break;

            case AiRequestTypes.Reco_RelatedPosts:
                await HandleRecoRelatedPostsStub(envelope, ct);
                break;

            case AiRequestTypes.Reco_UserToUserMatch:
                await HandleRecoUserToUserMatchStub(envelope, ct);
                break;

            // =========================================================================
            // 09) Negotiation / Pricing (STUBS)
            // =========================================================================
            case AiRequestTypes.Negotiation_PriceSuggestion:
                await HandleNegotiationPriceSuggestionStub(envelope, ct);
                break;

            case AiRequestTypes.Negotiation_CounterOfferSuggestion:
                await HandleNegotiationCounterOfferStub(envelope, ct);
                break;

            // =========================================================================
            // 10) Insights / Analytics (STUBS)
            // =========================================================================
            case AiRequestTypes.Insights_MarketTrends:
                await HandleInsightsMarketTrendsStub(envelope, ct);
                break;

            case AiRequestTypes.Insights_DemandPrediction:
                await HandleInsightsDemandPredictionStub(envelope, ct);
                break;

            case AiRequestTypes.Insights_UserBehaviorSummary:
                await HandleInsightsUserBehaviorStub(envelope, ct);
                break;

            // =========================================================================
            // 11) Contracts / Legal Assist (STUBS)
            // =========================================================================
            case AiRequestTypes.Contract_RiskFlags:
                await HandleContractRiskFlagsStub(envelope, ct);
                break;

            case AiRequestTypes.Contract_ClauseSuggestion:
                await HandleContractClauseSuggestionStub(envelope, ct);
                break;

            // =========================================================================
            // 12) Support / Operations (STUBS)
            // =========================================================================
            case AiRequestTypes.Support_AutoReplySuggestion:
                await HandleSupportAutoReplyStub(envelope, ct);
                break;

            case AiRequestTypes.Support_TicketClassification:
                await HandleSupportTicketClassificationStub(envelope, ct);
                break;

            case AiRequestTypes.Support_PriorityScoring:
                await HandleSupportPriorityScoringStub(envelope, ct);
                break;

            default:
                // Unknown requestType: ignore (or log)
                break;
        }
    }

    // -------------------------
    // JSON helper
    // -------------------------
    private static T? Deserialize<T>(JsonElement payload)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(
                payload.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch
        {
            return default;
        }
    }

    // =========================================================================
    // EXISTING HANDLERS (DB mapping already used in your schema)
    // =========================================================================

    private async Task HandleUserNidResult(AiResultEnvelope env, CancellationToken ct)
    {
        // Expect: env.Entity.Type == "user"
        // Result: AiDecisionResult.Decision -> AIDecision
        if (env.Entity.Type != "user") return;

        var result = Deserialize<AiDecisionResult>(env.Payload);
        if (result == null) return;

        var user = await _db.Set<User>().FindAsync(new object[] { env.Entity.Id }, ct);
        if (user == null) return;

        user.NIDEvaluation = (AIDecision)result.Decision;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleLandlordOwnershipResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "landlord") return;

        var result = Deserialize<AiDecisionResult>(env.Payload);
        if (result == null) return;

        var landlord = await _db.Set<Landlord>().FindAsync(new object[] { env.Entity.Id }, ct);
        if (landlord == null) return;

        landlord.OwnershipDocPathEvaluation = (AIDecision)result.Decision;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleCompanyRegisterResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "company") return;

        var result = Deserialize<AiDecisionResult>(env.Payload);
        if (result == null) return;

        // IMPORTANT: Company PK عندك هو UserId
        var company = await _db.Companies.FindAsync(new object[] { env.Entity.Id }, ct);
        if (company == null) return;

        company.CommercialRegisterEvaluation = (AIDecision)result.Decision;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandlePostFraudResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "post") return;

        var result = Deserialize<AiDecisionResult>(env.Payload);
        if (result == null) return;

        var post = await _db.Set<Post>().FindAsync(new object[] { env.Entity.Id }, ct);
        if (post == null) return;

        post.PostDocPathEvaluation = (AIDecision)result.Decision;

        // Map to pending status
        post.PendingStatus = result.Decision switch
        {
            (int)AIDecision.Verified => PostPendingStatus.Accepted,
            (int)AIDecision.Fraudulent => PostPendingStatus.Refused,
            _ => PostPendingStatus.Pending
        };

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandlePostImageManipulationResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "post") return;

        var result = Deserialize<AiDecisionResult>(env.Payload);
        if (result == null) return;

        var post = await _db.Set<Post>().FindAsync(new object[] { env.Entity.Id }, ct);
        if (post == null) return;

        // NOTE: انت حالياً بتخزنها في نفس PostDocPathEvaluation
        post.PostDocPathEvaluation = (AIDecision)result.Decision;

        if (result.Decision == (int)AIDecision.Fraudulent)
            post.PendingStatus = PostPendingStatus.Pending;

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandlePostPriceResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "post") return;

        var result = Deserialize<PriceEvaluationResult>(env.Payload);
        if (result == null) return;

        var post = await _db.Set<Post>().FindAsync(new object[] { env.Entity.Id }, ct);
        if (post == null) return;

        post.PriceEvaluation = (PriceEvaluation)result.PriceEvaluation;

        // Example rule: extreme prices => keep pending
        if (post.PriceEvaluation is PriceEvaluation.VeryHigh or PriceEvaluation.VeryLow)
            post.PendingStatus = PostPendingStatus.Pending;

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleInstallmentEligibilityResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "proposal") return;

        var result = Deserialize<EligibilityResult>(env.Payload);
        if (result == null) return;

        var proposal = await _db.Set<Proposal>().FindAsync(new object[] { env.Entity.Id }, ct);
        if (proposal == null) return;

        proposal.IsAble = (AIInstallmentDecision)result.IsAble;
        proposal.EligibilityAssessedAt = DateTime.UtcNow;
        proposal.EligibilityReason = result.Reason;
        proposal.EligibilityScore = result.Score;

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleRentEligibilityResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "proposal") return;

        var result = Deserialize<EligibilityResult>(env.Payload);
        if (result == null) return;

        var proposal = await _db.Set<Proposal>().FindAsync(new object[] { env.Entity.Id }, ct);
        if (proposal == null) return;

        proposal.RentIsAble = (AIRentDecision)result.IsAble;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandlePaymentFraudResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "payment_card") return;

        var result = Deserialize<AiDecisionResult>(env.Payload);
        if (result == null) return;

        var card = await _db.Set<PaymentCard>().FindAsync(new object[] { env.Entity.Id }, ct);
        if (card == null) return;

        if (result.Decision == (int)AIDecision.Fraudulent)
        {
            card.IsActive = false;

            _db.Set<Notification>().Add(new Notification
            {
                UserId = card.UserId,
                Content = "Your payment method was flagged as suspicious. Please update your card.",
                ReadStatus = false,
                CreatedAt = DateTime.Now
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleContentModerationResult(AiResultEnvelope env, CancellationToken ct)
    {
        var result = Deserialize<ContentModerationResult>(env.Payload);
        if (result == null) return;

        // Moderation for posts
        if (env.Entity.Type == "post")
        {
            var post = await _db.Set<Post>().FindAsync(new object[] { env.Entity.Id }, ct);
            if (post == null) return;

            if (!result.IsAllowed)
                post.PendingStatus = PostPendingStatus.Refused;

            await _db.SaveChangesAsync(ct);
            return;
        }

        // Moderation for complaints
        if (env.Entity.Type == "complaint")
        {
            var complaint = await _db.Set<Complaint>().FindAsync(new object[] { env.Entity.Id }, ct);
            if (complaint == null) return;

            // NOTE: عندك Status nullable enum => ComplaintStatus?
            complaint.Status = result.IsAllowed ? ComplaintStatus.Rejected : ComplaintStatus.Pending;

            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task HandleSmartReportResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "complaint") return;

        var result = Deserialize<AiDecisionResult>(env.Payload);
        if (result == null) return;

        var complaint = await _db.Set<Complaint>().FindAsync(new object[] { env.Entity.Id }, ct);
        if (complaint == null) return;

        complaint.Status = result.Decision switch
        {
            (int)AIDecision.Fraudulent => ComplaintStatus.Rejected,
            (int)AIDecision.Verified => ComplaintStatus.Pending,
            _ => ComplaintStatus.Pending
        };

        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleUserAnomalyResult(AiResultEnvelope env, CancellationToken ct)
    {
        if (env.Entity.Type != "user") return;

        var result = Deserialize<AnomalyResult>(env.Payload);
        if (result == null) return;

        if (!result.IsSuspicious) return;

        // notify first admin found (your current logic)
        var adminUserId = await _db.Set<Admin>()
            .Select(a => a.UserId)
            .FirstOrDefaultAsync(ct);

        if (adminUserId == 0) return;

        _db.Set<Notification>().Add(new Notification
        {
            UserId = adminUserId,
            Content = $"Suspicious activity detected for UserId={env.Entity.Id}. Score={result.Score:0.00}. Reason={result.Reason}",
            ReadStatus = false,
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync(ct);
    }

    // =========================================================================
    // STUB HANDLERS (NO DB FIELDS YET) - YOU WILL MAP LATER
    // Each stub is intentionally "NO-OP" so project compiles end-to-end.
    // =========================================================================

    private Task HandleContentSpamDetectionStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO:
        // - Decide which entity types are moderated (post/comment/message/complaint)
        // - Add DB columns/table to store spam score/label
        // - Deserialize<TextAnalysisResult> or AiDecisionResult based on AI contract
        return Task.CompletedTask;
    }

    private Task HandleContentToxicityStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store toxicity score / maybe auto-refuse posts/comments
        return Task.CompletedTask;
    }

    private Task HandleContentSentimentStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store sentiment (useful for analytics/insights)
        return Task.CompletedTask;
    }

    private Task HandleContentLanguageStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store detected language (e.g., "ar", "en")
        return Task.CompletedTask;
    }

    private Task HandleSearchQueryUnderstandingStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store query intent/entities (usually not stored in DB, can be transient cache)
        return Task.CompletedTask;
    }

    private Task HandleSearchSemanticRankingStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store ranked ids in cache OR return directly to caller (not DB)
        return Task.CompletedTask;
    }

    private Task HandleSearchSimilarListingsStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store "similar posts" graph OR compute on the fly
        return Task.CompletedTask;
    }

    private Task HandleRecoPersonalizedFeedStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store feed recommendations per user (table: UserRecommendations)
        return Task.CompletedTask;
    }

    private Task HandleRecoRelatedPostsStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store related posts per post
        return Task.CompletedTask;
    }

    private Task HandleRecoUserToUserMatchStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store match suggestions (tenant-landlord matching etc.)
        return Task.CompletedTask;
    }

    private Task HandleNegotiationPriceSuggestionStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store suggested price for a post/proposal/transaction
        return Task.CompletedTask;
    }

    private Task HandleNegotiationCounterOfferStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store suggested counter-offer message / price
        return Task.CompletedTask;
    }

    private Task HandleInsightsMarketTrendsStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store aggregated trends (table: MarketInsights)
        return Task.CompletedTask;
    }

    private Task HandleInsightsDemandPredictionStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store demand prediction per area/category/time
        return Task.CompletedTask;
    }

    private Task HandleInsightsUserBehaviorStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store user behavior summary for admin dashboards
        return Task.CompletedTask;
    }

    private Task HandleContractRiskFlagsStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: link to Contract entity (env.Entity.Type == "contract") and store risk flags
        return Task.CompletedTask;
    }

    private Task HandleContractClauseSuggestionStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store suggested clauses or generate PDF diff
        return Task.CompletedTask;
    }

    private Task HandleSupportAutoReplyStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store suggested reply for complaint/support ticket
        return Task.CompletedTask;
    }

    private Task HandleSupportTicketClassificationStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store classification label per complaint/support ticket
        return Task.CompletedTask;
    }

    private Task HandleSupportPriorityScoringStub(AiResultEnvelope env, CancellationToken ct)
    {
        // TODO: store priority score per complaint/support ticket
        return Task.CompletedTask;
    }
}
