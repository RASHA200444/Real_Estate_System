using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using otherServices.Infrastructure.Kafka.Models;
using otherServices.Models;
using otherServices.Models.Enums;

namespace otherServices.Infrastructure.Kafka
{
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
            if (string.IsNullOrWhiteSpace(envelope.RequestId)) return;
            if (string.IsNullOrWhiteSpace(envelope.RequestType)) return;
            if (envelope.Entity == null) return;

            // =========================
            // 0) Idempotency (RequestId unique)
            // =========================
            var alreadyHandled = await _db.Set<AiModuleResult>()
                .AnyAsync(x => x.RequestId == envelope.RequestId, ct);

            if (alreadyHandled)
                return;

            // =========================
            // 1) Always store raw result in AiModuleResults (audit/history)
            // =========================
            var rawJson = envelope.Payload.ValueKind == JsonValueKind.Undefined
                ? "{}"
                : envelope.Payload.GetRawText();

            var moduleRow = new AiModuleResult
            {
                RequestId = envelope.RequestId,
                RequestType = envelope.RequestType,
                EntityType = envelope.Entity.Type,
                EntityId = envelope.Entity.Id,
                PayloadJson = rawJson,
                CreatedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow
            };

            // We will fill DecisionInt/Score/Reason if we can
            // (helps querying without parsing json)
            await FillNormalizedFieldsIfPossible(moduleRow, envelope);

            _db.Set<AiModuleResult>().Add(moduleRow);

            // =========================
            // 2) Route to DB updates (core tables)
            // =========================
            switch (envelope.RequestType)
            {
                // =========================================================================
                // 01) Fraud / Documents (3)
                // =========================================================================
                case AiRequestTypes.Fraud_DocumentAnalysis:
                    await HandleFraudDocumentAnalysis(envelope, ct);
                    break;

                case AiRequestTypes.Fraud_OwnershipDocumentAnalysis:
                    await HandleFraudOwnershipDocumentAnalysis(envelope, ct);
                    break;

                case AiRequestTypes.Fraud_CommercialRegisterAnalysis:
                    await HandleFraudCommercialRegisterAnalysis(envelope, ct);
                    break;

                // =========================================================================
                // 02) Fraud / Posts (3)
                // =========================================================================
                case AiRequestTypes.Fraud_FakePropertyDetection:
                    await HandleFraudFakePropertyDetection(envelope, ct);
                    break;

                case AiRequestTypes.Fraud_ImageManipulation:
                    await HandleFraudImageManipulation(envelope, ct);
                    break;

                case AiRequestTypes.Price_AnomalyDetection:
                    await HandlePriceAnomalyDetection(envelope, ct);
                    break;

                // =========================================================================
                // 03) Buyer / Proposals (2)
                // =========================================================================
                case AiRequestTypes.Buyer_InstallmentRisk:
                    await HandleBuyerInstallmentRisk(envelope, ct);
                    break;

                case AiRequestTypes.Buyer_RentEligibility:
                    await HandleBuyerRentEligibility(envelope, ct);
                    break;

                // =========================================================================
                // 04) Payment (1)
                // =========================================================================
                case AiRequestTypes.Payment_FraudDetection:
                    await HandlePaymentFraudDetection(envelope, ct);
                    break;

                // =========================================================================
                // 05) Content / Reports / Anomaly (3)
                // =========================================================================
                case AiRequestTypes.Content_Moderation:
                    await HandleContentModeration(envelope, ct);
                    break;

                case AiRequestTypes.Smart_ReportsAnalysis:
                    await HandleReportsSmartAnalysis(envelope, ct);
                    break;

                case AiRequestTypes.User_AnomalyDetection:
                    await HandleUserAnomalyDetection(envelope, ct);
                    break;

                // =========================================================================
                // 06) Content / Text (4)
                // =========================================================================
                case AiRequestTypes.Content_SpamDetection:
                    await HandleContentSpamDetection(envelope, ct);
                    break;

                case AiRequestTypes.Content_ToxicityScoring:
                    await HandleContentToxicityScoring(envelope, ct);
                    break;

                case AiRequestTypes.Content_SentimentAnalysis:
                    await HandleContentSentimentAnalysis(envelope, ct);
                    break;

                case AiRequestTypes.Content_LanguageDetection:
                    await HandleContentLanguageDetection(envelope, ct);
                    break;

                // =========================================================================
                // 07) Search / Retrieval (3)
                // =========================================================================
                case AiRequestTypes.Search_QueryUnderstanding:
                    await HandleSearchQueryUnderstanding(envelope, ct);
                    break;

                case AiRequestTypes.Search_SemanticRanking:
                    await HandleSearchSemanticRanking(envelope, ct);
                    break;

                case AiRequestTypes.Search_SimilarListings:
                    await HandleSearchSimilarListings(envelope, ct);
                    break;

                // =========================================================================
                // 08) Recommendation / Personalization (3)
                // =========================================================================
                case AiRequestTypes.Reco_PersonalizedFeed:
                    await HandleRecoPersonalizedFeed(envelope, ct);
                    break;

                case AiRequestTypes.Reco_RelatedPosts:
                    await HandleRecoRelatedPosts(envelope, ct);
                    break;

                case AiRequestTypes.Reco_UserToUserMatch:
                    await HandleRecoUserToUserMatch(envelope, ct);
                    break;

                // =========================================================================
                // 09) Negotiation / Pricing (2)
                // =========================================================================
                case AiRequestTypes.Negotiation_PriceSuggestion:
                    await HandleNegotiationPriceSuggestion(envelope, ct);
                    break;

                case AiRequestTypes.Negotiation_CounterOfferSuggestion:
                    await HandleNegotiationCounterOfferSuggestion(envelope, ct);
                    break;

                // =========================================================================
                // 10) Insights / Analytics (3)
                // =========================================================================
                case AiRequestTypes.Insights_MarketTrends:
                    await HandleInsightsMarketTrends(envelope, ct);
                    break;

                case AiRequestTypes.Insights_DemandPrediction:
                    await HandleInsightsDemandPrediction(envelope, ct);
                    break;

                case AiRequestTypes.Insights_UserBehaviorSummary:
                    await HandleInsightsUserBehaviorSummary(envelope, ct);
                    break;

                // =========================================================================
                // 11) Contracts / Legal Assist (2)
                // =========================================================================
                case AiRequestTypes.Contract_RiskFlags:
                    await HandleContractRiskFlags(envelope, ct);
                    break;

                case AiRequestTypes.Contract_ClauseSuggestion:
                    await HandleContractClauseSuggestion(envelope, ct);
                    break;

                // =========================================================================
                // 12) Support / Operations (3)
                // =========================================================================
                case AiRequestTypes.Support_AutoReplySuggestion:
                    await HandleSupportAutoReplySuggestion(envelope, ct);
                    break;

                case AiRequestTypes.Support_TicketClassification:
                    await HandleSupportTicketClassification(envelope, ct);
                    break;

                case AiRequestTypes.Support_PriorityScoring:
                    await HandleSupportPriorityScoring(envelope, ct);
                    break;

                default:
                    // unknown request type -> only stored in AiModuleResults
                    break;
            }

            // Save both AiModuleResults + any table updates in ONE transaction
            await _db.SaveChangesAsync(ct);
        }

        // ============================================================
        // Helpers
        // ============================================================

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

        /// <summary>
        /// Tries to fill DecisionInt / Score / Reason from payload (if fields exist).
        /// This is optional but very useful for querying AiModuleResults quickly.
        /// </summary>
        private async Task FillNormalizedFieldsIfPossible(AiModuleResult row, AiResultEnvelope env)
        {
            // We will try lightweight parsing based on RequestType.
            // (No DB calls here; just payload normalization.)
            switch (env.RequestType)
            {
                case AiRequestTypes.Fraud_DocumentAnalysis:
                    {
                        var r = Deserialize<FraudDocumentAnalysisResult>(env.Payload);
                        if (r != null) { row.DecisionInt = r.Decision; row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.Fraud_OwnershipDocumentAnalysis:
                    {
                        var r = Deserialize<FraudOwnershipDocumentAnalysisResult>(env.Payload);
                        if (r != null) { row.DecisionInt = r.Decision; row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.Fraud_CommercialRegisterAnalysis:
                    {
                        var r = Deserialize<FraudCommercialRegisterAnalysisResult>(env.Payload);
                        if (r != null) { row.DecisionInt = r.Decision; row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.Fraud_FakePropertyDetection:
                    {
                        var r = Deserialize<FraudFakePropertyDetectionResult>(env.Payload);
                        if (r != null) { row.DecisionInt = r.Decision; row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.Fraud_ImageManipulation:
                    {
                        var r = Deserialize<FraudImageManipulationResult>(env.Payload);
                        if (r != null) { row.DecisionInt = r.Decision; row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.Price_AnomalyDetection:
                    {
                        var r = Deserialize<PriceAnomalyDetectionResult>(env.Payload);
                        if (r != null) { row.DecisionInt = r.PriceEvaluation; row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.Buyer_InstallmentRisk:
                    {
                        var r = Deserialize<BuyerInstallmentRiskResult>(env.Payload);
                        if (r != null) { row.DecisionInt = r.IsAble; row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.Buyer_RentEligibility:
                    {
                        var r = Deserialize<BuyerRentEligibilityResult>(env.Payload);
                        if (r != null) { row.DecisionInt = r.IsAble; row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.Payment_FraudDetection:
                    {
                        var r = Deserialize<PaymentFraudDetectionResult>(env.Payload);
                        if (r != null)
                        {
                            row.DecisionInt = r.Decision;
                            // If AI sends score (int?) keep it as double
                            row.Score = r.Score.HasValue ? Convert.ToDouble(r.Score.Value) : r.Confidence;
                            row.Reason = r.Reason;
                        }
                        break;
                    }
                case AiRequestTypes.Content_Moderation:
                    {
                        var r = Deserialize<ContentModerationResult>(env.Payload);
                        if (r != null) { row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.Smart_ReportsAnalysis:
                    {
                        var r = Deserialize<ReportsSmartAnalysisResult>(env.Payload);
                        if (r != null)
                        {
                            row.DecisionInt = r.Decision;
                            row.Score = r.Confidence;
                            row.Reason = r.Reason;
                        }
                        break;
                    }
                case AiRequestTypes.User_AnomalyDetection:
                    {
                        var r = Deserialize<UserAnomalyDetectionResult>(env.Payload);
                        if (r != null)
                        {
                            row.Score = r.Score; // anomaly score
                            row.Reason = r.Reason;
                        }
                        break;
                    }

                default:
                    // For future payloads we keep only raw json
                    break;
            }

            await Task.CompletedTask;
        }

        // ============================================================
        // 01) Fraud / Documents (3)
        // ============================================================

        private async Task HandleFraudDocumentAnalysis(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "user") return;

            var result = Deserialize<FraudDocumentAnalysisResult>(env.Payload);
            if (result == null) return;

            var user = await _db.Users.FindAsync(new object[] { env.Entity.Id }, ct);
            if (user == null) return;

            user.NIDEvaluation = (AIDecision)result.Decision;
        }

        private async Task HandleFraudOwnershipDocumentAnalysis(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "landlord") return;

            var result = Deserialize<FraudOwnershipDocumentAnalysisResult>(env.Payload);
            if (result == null) return;

            var landlord = await _db.Landlords.FindAsync(new object[] { env.Entity.Id }, ct);
            if (landlord == null) return;

            landlord.OwnershipDocPathEvaluation = (AIDecision)result.Decision;
        }

        private async Task HandleFraudCommercialRegisterAnalysis(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "company") return;

            var result = Deserialize<FraudCommercialRegisterAnalysisResult>(env.Payload);
            if (result == null) return;

            // Company PK = UserId
            var company = await _db.Companies.FindAsync(new object[] { env.Entity.Id }, ct);
            if (company == null) return;

            company.CommercialRegisterEvaluation = (AIDecision)result.Decision;
        }

        // ============================================================
        // 02) Fraud / Posts (3)
        // ============================================================

        private async Task HandleFraudFakePropertyDetection(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "post") return;

            var result = Deserialize<FraudFakePropertyDetectionResult>(env.Payload);
            if (result == null) return;

            var post = await _db.Posts.FindAsync(new object[] { env.Entity.Id }, ct);
            if (post == null) return;

            post.FakePropertyEvaluation = (AIDecision)result.Decision;
            post.AiConfidence = result.Confidence;
            post.AiReason = result.Reason;
            post.AiLastCheckedAt = DateTime.UtcNow;

            // Optional: update PendingStatus based on decision
            post.PendingStatus = result.Decision switch
            {
                (int)AIDecision.Verified => PostPendingStatus.Accepted,
                (int)AIDecision.Fraudulent => PostPendingStatus.Refused,
                _ => PostPendingStatus.Pending
            };
        }

        private async Task HandleFraudImageManipulation(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "post") return;

            var result = Deserialize<FraudImageManipulationResult>(env.Payload);
            if (result == null) return;

            var post = await _db.Posts.FindAsync(new object[] { env.Entity.Id }, ct);
            if (post == null) return;

            post.ImageManipulationEvaluation = (AIDecision)result.Decision;
            post.AiConfidence = result.Confidence;
            post.AiReason = result.Reason;
            post.AiLastCheckedAt = DateTime.UtcNow;

            if (result.Decision == (int)AIDecision.Fraudulent)
                post.PendingStatus = PostPendingStatus.Pending; // you can also choose Refused
        }

        private async Task HandlePriceAnomalyDetection(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "post") return;

            var result = Deserialize<PriceAnomalyDetectionResult>(env.Payload);
            if (result == null) return;

            var post = await _db.Posts.FindAsync(new object[] { env.Entity.Id }, ct);
            if (post == null) return;

            post.PriceEvaluation = (PriceEvaluation)result.PriceEvaluation;

            post.AiConfidence = result.Confidence;
            post.AiReason = result.Reason;
            post.AiLastCheckedAt = DateTime.UtcNow;

            if (post.PriceEvaluation is PriceEvaluation.VeryHigh or PriceEvaluation.VeryLow)
                post.PendingStatus = PostPendingStatus.Pending;
        }

        // ============================================================
        // 03) Buyer / Proposals (2)
        // ============================================================

        private async Task HandleBuyerInstallmentRisk(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "proposal") return;

            var result = Deserialize<BuyerInstallmentRiskResult>(env.Payload);
            if (result == null) return;

            var proposal = await _db.Proposals.FindAsync(new object[] { env.Entity.Id }, ct);
            if (proposal == null) return;

            proposal.IsAble = (AIInstallmentDecision)result.IsAble;
            proposal.EligibilityScore = result.Score;
            proposal.EligibilityReason = result.Reason;
            proposal.EligibilityAssessedAt = DateTime.UtcNow;
        }

        private async Task HandleBuyerRentEligibility(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "proposal") return;

            var result = Deserialize<BuyerRentEligibilityResult>(env.Payload);
            if (result == null) return;

            var proposal = await _db.Proposals.FindAsync(new object[] { env.Entity.Id }, ct);
            if (proposal == null) return;

            proposal.RentIsAble = (AIRentDecision)result.IsAble;
            proposal.RentEligibilityScore = result.Score;
            proposal.RentEligibilityReason = result.Reason;
            proposal.RentEligibilityAssessedAt = DateTime.UtcNow;
        }

        // ============================================================
        // 04) Payment (1)
        // ============================================================

        private async Task HandlePaymentFraudDetection(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "payment_card") return;

            var result = Deserialize<PaymentFraudDetectionResult>(env.Payload);
            if (result == null) return;

            var card = await _db.PaymentCards.FindAsync(new object[] { env.Entity.Id }, ct);
            if (card == null) return;

            // store details
            card.FraudScore = result.Score.HasValue ? Convert.ToDouble(result.Score.Value) : (double?)null;
            card.FraudReason = result.Reason;
            card.FraudAssessedAt = DateTime.UtcNow;

            // action
            if (result.Decision == (int)AIDecision.Fraudulent)
            {
                card.IsActive = false;

                _db.Notifications.Add(new Notification
                {
                    UserId = card.UserId,
                    Content = "Your payment method was flagged as suspicious. Please update your card.",
                    ReadStatus = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // ============================================================
        // 05) Content / Reports / Anomaly (3)
        // ============================================================

        private async Task HandleContentModeration(AiResultEnvelope env, CancellationToken ct)
        {
            var result = Deserialize<ContentModerationResult>(env.Payload);
            if (result == null) return;

            // If moderation affects a post:
            if (env.Entity.Type == "post")
            {
                var post = await _db.Posts.FindAsync(new object[] { env.Entity.Id }, ct);
                if (post == null) return;

                if (!result.IsAllowed)
                    post.PendingStatus = PostPendingStatus.Refused;

                // store meta on post too
                post.AiConfidence = result.Confidence;
                post.AiReason = result.Reason;
                post.AiLastCheckedAt = DateTime.UtcNow;
                return;
            }

            // If moderation affects a complaint:
            if (env.Entity.Type == "complaint")
            {
                var complaint = await _db.Complaints.FindAsync(new object[] { env.Entity.Id }, ct);
                if (complaint == null) return;

                complaint.AiSeverity = result.Severity;
                complaint.AiReason = result.Reason;
                complaint.AiAssessedAt = DateTime.UtcNow;

                // You can keep status logic as you want:
                // If not allowed => keep pending for admin review, else maybe reject the complaint
                complaint.Status = result.IsAllowed ? ComplaintStatus.Rejected : ComplaintStatus.Pending;
            }
        }

        private async Task HandleReportsSmartAnalysis(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "complaint") return;

            var result = Deserialize<ReportsSmartAnalysisResult>(env.Payload);
            if (result == null) return;

            var complaint = await _db.Complaints.FindAsync(new object[] { env.Entity.Id }, ct);
            if (complaint == null) return;

            complaint.AiSeverity = result.Severity;
            complaint.AiReason = result.Reason;
            complaint.AiAssessedAt = DateTime.UtcNow;

            // Decision => you can map however you want
            // example:
            complaint.Status = result.Decision switch
            {
                (int)AIDecision.Fraudulent => ComplaintStatus.Rejected,
                (int)AIDecision.Verified => ComplaintStatus.Pending,
                _ => ComplaintStatus.Pending
            };
        }

        private async Task HandleUserAnomalyDetection(AiResultEnvelope env, CancellationToken ct)
        {
            var result = Deserialize<UserAnomalyDetectionResult>(env.Payload);
            if (result == null) return;

            if (!result.IsSuspicious) return;

            // Apply anomaly to entity (user/landlord/company)
            if (env.Entity.Type == "user")
            {
                var user = await _db.Users.FindAsync(new object[] { env.Entity.Id }, ct);
                if (user == null) return;

                user.AnomalyScore = result.Score;
                user.AnomalyReason = result.Reason;
                user.AnomalyFlaggedAt = DateTime.UtcNow;

                await NotifyAdminIfExists($"Suspicious activity detected for UserId={env.Entity.Id}. Score={result.Score:0.00}. Reason={result.Reason}", ct);
                return;
            }

            if (env.Entity.Type == "landlord")
            {
                var landlord = await _db.Landlords.FindAsync(new object[] { env.Entity.Id }, ct);
                if (landlord == null) return;

                landlord.AnomalyScore = result.Score;
                landlord.AnomalyReason = result.Reason;
                landlord.AnomalyFlaggedAt = DateTime.UtcNow;

                await NotifyAdminIfExists($"Suspicious activity detected for LandlordId={env.Entity.Id}. Score={result.Score:0.00}. Reason={result.Reason}", ct);
                return;
            }

            if (env.Entity.Type == "company")
            {
                var company = await _db.Companies.FindAsync(new object[] { env.Entity.Id }, ct);
                if (company == null) return;

                company.AnomalyScore = result.Score;
                company.AnomalyReason = result.Reason;
                company.AnomalyFlaggedAt = DateTime.UtcNow;

                await NotifyAdminIfExists($"Suspicious activity detected for Company(UserId)={env.Entity.Id}. Score={result.Score:0.00}. Reason={result.Reason}", ct);
            }
        }

        private async Task NotifyAdminIfExists(string message, CancellationToken ct)
        {
            var adminUserId = await _db.Admins
                .Select(a => a.UserId)
                .FirstOrDefaultAsync(ct);

            if (adminUserId == 0) return;

            _db.Notifications.Add(new Notification
            {
                UserId = adminUserId,
                Content = message,
                ReadStatus = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        // ============================================================
        // 06) Content / Text (4)
        // We store in AiModuleResults always.
        // We also store into Complaint fields if entity=complaint.
        // For posts, we update AiReason/Confidence/LastCheckedAt.
        // ============================================================

        private async Task HandleContentSpamDetection(AiResultEnvelope env, CancellationToken ct)
        {
            var result = Deserialize<ContentSpamDetectionResult>(env.Payload);
            if (result == null) return;

            if (env.Entity.Type == "post")
            {
                var post = await _db.Posts.FindAsync(new object[] { env.Entity.Id }, ct);
                if (post == null) return;

                post.AiConfidence = result.Confidence;
                post.AiReason = $"Spam={result.IsSpam}, Score={result.Score:0.00}. {result.Reason}";
                post.AiLastCheckedAt = DateTime.UtcNow;

                if (result.IsSpam)
                    post.PendingStatus = PostPendingStatus.Refused;
            }
            else if (env.Entity.Type == "complaint")
            {
                var complaint = await _db.Complaints.FindAsync(new object[] { env.Entity.Id }, ct);
                if (complaint == null) return;

                complaint.AiSeverity = (int)Math.Round(result.Score * 100);
                complaint.AiReason = $"Spam={result.IsSpam}. {result.Reason}";
                complaint.AiAssessedAt = DateTime.UtcNow;
            }
        }

        private async Task HandleContentToxicityScoring(AiResultEnvelope env, CancellationToken ct)
        {
            var result = Deserialize<ContentToxicityScoringResult>(env.Payload);
            if (result == null) return;

            if (env.Entity.Type == "post")
            {
                var post = await _db.Posts.FindAsync(new object[] { env.Entity.Id }, ct);
                if (post == null) return;

                post.AiConfidence = result.Confidence;
                post.AiReason = $"Toxicity={result.ToxicityScore:0.00}, Severity={result.Severity}. {result.Reason}";
                post.AiLastCheckedAt = DateTime.UtcNow;

                // Example policy: high toxicity => refuse
                if (result.Severity.HasValue && result.Severity.Value >= 4)
                    post.PendingStatus = PostPendingStatus.Refused;
            }
            else if (env.Entity.Type == "complaint")
            {
                var complaint = await _db.Complaints.FindAsync(new object[] { env.Entity.Id }, ct);
                if (complaint == null) return;

                complaint.AiSeverity = result.Severity;
                complaint.AiReason = $"Toxicity={result.ToxicityScore:0.00}. {result.Reason}";
                complaint.AiAssessedAt = DateTime.UtcNow;
            }
        }

        private async Task HandleContentSentimentAnalysis(AiResultEnvelope env, CancellationToken ct)
        {
            var result = Deserialize<ContentSentimentAnalysisResult>(env.Payload);
            if (result == null) return;

            // Mostly analytics; we will store in AiModuleResults only.
            // If you want, attach to posts too:
            if (env.Entity.Type == "post")
            {
                var post = await _db.Posts.FindAsync(new object[] { env.Entity.Id }, ct);
                if (post == null) return;

                post.AiConfidence = result.Confidence;
                post.AiReason = $"Sentiment={result.Label}, Score={result.Score:0.00}. {result.Reason}";
                post.AiLastCheckedAt = DateTime.UtcNow;
            }
        }

        private async Task HandleContentLanguageDetection(AiResultEnvelope env, CancellationToken ct)
        {
            var result = Deserialize<ContentLanguageDetectionResult>(env.Payload);
            if (result == null) return;

            // No direct DB columns for language now
            // Stored in AiModuleResults already
            await Task.CompletedTask;
        }

        // ============================================================
        // 07) Search / Retrieval (3) - Stored in AiModuleResults only
        // ============================================================

        private Task HandleSearchQueryUnderstanding(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleSearchSemanticRanking(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleSearchSimilarListings(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        // ============================================================
        // 08) Recommendation / Personalization (3) - Stored in AiModuleResults only
        // ============================================================

        private Task HandleRecoPersonalizedFeed(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleRecoRelatedPosts(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleRecoUserToUserMatch(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        // ============================================================
        // 09) Negotiation / Pricing (2) - Stored in AiModuleResults only
        // ============================================================

        private Task HandleNegotiationPriceSuggestion(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleNegotiationCounterOfferSuggestion(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        // ============================================================
        // 10) Insights / Analytics (3) - Stored in AiModuleResults only
        // ============================================================

        private Task HandleInsightsMarketTrends(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleInsightsDemandPrediction(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleInsightsUserBehaviorSummary(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        // ============================================================
        // 11) Contracts / Legal Assist (2) - Stored in AiModuleResults only
        // ============================================================

        private Task HandleContractRiskFlags(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleContractClauseSuggestion(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        // ============================================================
        // 12) Support / Operations (3) - Stored in AiModuleResults only
        // ============================================================

        private Task HandleSupportAutoReplySuggestion(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleSupportTicketClassification(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleSupportPriorityScoring(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
    }
}
