using System;
using System.Linq;
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

            // 0) Idempotency
            var alreadyHandled = await _db.Set<AiModuleResult>()
                .AnyAsync(x => x.RequestId == envelope.RequestId, ct);

            if (alreadyHandled)
                return;

            // 1) Always store raw
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

            await FillNormalizedFieldsIfPossible(moduleRow, envelope);
            _db.Set<AiModuleResult>().Add(moduleRow);

            // 2) Route to DB updates (NO post PendingStatus changes here)
            switch (envelope.RequestType)
            {
                // 01) Fraud / Documents (3)
                case AiRequestTypes.Fraud_DocumentAnalysis:
                    await HandleFraudDocumentAnalysis(envelope, ct);
                    break;

                case AiRequestTypes.Fraud_OwnershipDocumentAnalysis:
                    await HandleFraudOwnershipDocumentAnalysis(envelope, ct);
                    break;

                case AiRequestTypes.Fraud_CommercialRegisterAnalysis:
                    await HandleFraudCommercialRegisterAnalysis(envelope, ct);
                    break;

                // ✅ NEW: Fraud / Projects doc analysis
                case "fraud.project_document_analysis":
                    await HandleFraudProjectDocumentAnalysis(envelope, ct);
                    break;

                // 02) Fraud / Posts (4)
                case AiRequestTypes.Fraud_FakePropertyDetection:
                    await HandleFraudFakePropertyDetection(envelope, ct);
                    break;

                case AiRequestTypes.Fraud_ImageManipulation:
                    await HandleFraudImageManipulation(envelope, ct);
                    break;

                case AiRequestTypes.Fraud_PostDocumentAnalysis:
                    await HandleFraudPostDocumentAnalysis(envelope, ct);
                    break;

                case AiRequestTypes.Price_AnomalyDetection:
                    await HandlePriceAnomalyDetection(envelope, ct);
                    break;

                // 03) Buyer / Proposals (2)
                case AiRequestTypes.Buyer_InstallmentRisk:
                    await HandleBuyerInstallmentRisk(envelope, ct);
                    break;

                case AiRequestTypes.Buyer_RentEligibility:
                    await HandleBuyerRentEligibility(envelope, ct);
                    break;

                // 04) Payment (1)
                case AiRequestTypes.Payment_FraudDetection:
                    await HandlePaymentFraudDetection(envelope, ct);
                    break;

                // 05) Content / Reports / Anomaly (3)
                case AiRequestTypes.Content_Moderation:
                    await HandleContentModeration(envelope, ct);
                    break;

                case AiRequestTypes.Smart_ReportsAnalysis:
                    await HandleReportsSmartAnalysis(envelope, ct);
                    break;

                case AiRequestTypes.User_AnomalyDetection:
                    await HandleUserAnomalyDetection(envelope, ct);
                    break;

                // 06) Content / Text (4)
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

                default:
                    break;
            }

            // Save all updates + AiModuleResult
            await _db.SaveChangesAsync(ct);

            // 3) FINALIZE post status (ONLY posts)
            if (envelope.Entity.Type == "post")
            {
                await FinalizePostStatusIfReady(envelope.Entity.Id, ct);
                await _db.SaveChangesAsync(ct);
            }
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

        private async Task FillNormalizedFieldsIfPossible(AiModuleResult row, AiResultEnvelope env)
        {
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

                // ✅ NEW
                case "fraud.project_document_analysis":
                    {
                        var r = Deserialize<FraudProjectDocumentAnalysisResult>(env.Payload);
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
                case AiRequestTypes.Fraud_PostDocumentAnalysis:
                    {
                        var r = Deserialize<FraudPostDocumentAnalysisResult>(env.Payload);
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
                        if (r != null) { row.DecisionInt = r.Decision; row.Score = r.Confidence; row.Reason = r.Reason; }
                        break;
                    }
                case AiRequestTypes.User_AnomalyDetection:
                    {
                        var r = Deserialize<UserAnomalyDetectionResult>(env.Payload);
                        if (r != null) { row.Score = r.Score; row.Reason = r.Reason; }
                        break;
                    }
                default:
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

            var company = await _db.Companies.FindAsync(new object[] { env.Entity.Id }, ct);
            if (company == null) return;

            company.CommercialRegisterEvaluation = (AIDecision)result.Decision;
        }

        // ============================================================
        // ✅ NEW: Projects (NO PendingStatus changes, admin only)
        // ============================================================

        private async Task HandleFraudProjectDocumentAnalysis(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "project") return;

            // هنا احنا مش هنغير أي fields في Project لأنه مفيهوش AI fields
            // بس لو حبيت بعدين تضيف: ProjectDocEvaluation / AiConfidence / AiReason ... ساعتها نحدثها هنا
            var project = await _db.Projects.FindAsync(new object[] { env.Entity.Id }, ct);
            if (project == null) return;

            await Task.CompletedTask;
        }

        // ============================================================
        // 02) Fraud / Posts (4)  (NO status updates here)
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
        }

        private async Task HandleFraudPostDocumentAnalysis(AiResultEnvelope env, CancellationToken ct)
        {
            if (env.Entity.Type != "post") return;

            var result = Deserialize<FraudPostDocumentAnalysisResult>(env.Payload);
            if (result == null) return;

            var post = await _db.Posts.FindAsync(new object[] { env.Entity.Id }, ct);
            if (post == null) return;

            post.PostDocPathEvaluation = (AIDecision)result.Decision;

            post.AiConfidence = result.Confidence;
            post.AiReason = result.Reason;
            post.AiLastCheckedAt = DateTime.UtcNow;
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

            card.FraudScore = result.Score.HasValue ? Convert.ToDouble(result.Score.Value) : (double?)null;
            card.FraudReason = result.Reason;
            card.FraudAssessedAt = DateTime.UtcNow;

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

            if (env.Entity.Type == "post")
            {
                var post = await _db.Posts.FindAsync(new object[] { env.Entity.Id }, ct);
                if (post == null) return;

                post.AiConfidence = result.Confidence;
                post.AiReason = result.Reason;
                post.AiLastCheckedAt = DateTime.UtcNow;
                return;
            }

            // ✅ NEW: project (informational only — stored in AiModuleResult already)
            if (env.Entity.Type == "project")
            {
                var project = await _db.Projects.FindAsync(new object[] { env.Entity.Id }, ct);
                if (project == null) return;

                // لا تغيّر PendingStatus
                return;
            }

            if (env.Entity.Type == "complaint")
            {
                var complaint = await _db.Complaints.FindAsync(new object[] { env.Entity.Id }, ct);
                if (complaint == null) return;

                complaint.AiSeverity = result.Severity;
                complaint.AiReason = result.Reason;
                complaint.AiAssessedAt = DateTime.UtcNow;

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

            await Task.CompletedTask;
        }

        // ============================================================
        // FINALIZER (ONLY place that changes Post.PendingStatus)
        // ============================================================

        private async Task FinalizePostStatusIfReady(long postId, CancellationToken ct)
        {
            var post = await _db.Posts
                .Include(p => p.PostImages)
                .FirstOrDefaultAsync(p => p.PostId == postId, ct);

            if (post == null) return;

            bool hasImages = post.PostImages != null && post.PostImages.Any();

            var required = new[]
            {
                AiRequestTypes.Fraud_PostDocumentAnalysis,
                AiRequestTypes.Fraud_FakePropertyDetection,
                AiRequestTypes.Price_AnomalyDetection,
                AiRequestTypes.Content_Moderation
            };

            bool allCoreArrived = await _db.Set<AiModuleResult>()
                .AsNoTracking()
                .Where(r => r.EntityType == "post" && r.EntityId == postId)
                .Where(r => required.Contains(r.RequestType))
                .Select(r => r.RequestType)
                .Distinct()
                .CountAsync(ct) == required.Length;

            if (!allCoreArrived)
                return;

            if (hasImages)
            {
                bool imgArrived = await _db.Set<AiModuleResult>()
                    .AsNoTracking()
                    .AnyAsync(r => r.EntityType == "post" && r.EntityId == postId
                                   && r.RequestType == AiRequestTypes.Fraud_ImageManipulation, ct);

                if (!imgArrived)
                    return;
            }

            bool isFraudulent =
                post.PostDocPathEvaluation == AIDecision.Fraudulent ||
                post.FakePropertyEvaluation == AIDecision.Fraudulent ||
                (hasImages && post.ImageManipulationEvaluation == AIDecision.Fraudulent);

            if (isFraudulent)
            {
                post.PendingStatus = PostPendingStatus.Refused;
                return;
            }

            var moderationRow = await _db.Set<AiModuleResult>()
                .AsNoTracking()
                .Where(r => r.EntityType == "post" && r.EntityId == postId
                            && r.RequestType == AiRequestTypes.Content_Moderation)
                .OrderByDescending(r => r.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (moderationRow != null)
            {
                try
                {
                    using var doc = JsonDocument.Parse(moderationRow.PayloadJson ?? "{}");
                    var cm = JsonSerializer.Deserialize<ContentModerationResult>(
                        doc.RootElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (cm != null && cm.IsAllowed == false)
                    {
                        post.PendingStatus = PostPendingStatus.Refused;
                        return;
                    }
                }
                catch
                {
                    post.PendingStatus = PostPendingStatus.Pending;
                    return;
                }
            }

            bool anyUncertain =
                post.PostDocPathEvaluation is AIDecision.Uncertain or AIDecision.NotReviewed ||
                post.FakePropertyEvaluation is AIDecision.Uncertain or AIDecision.NotReviewed ||
                (hasImages && (post.ImageManipulationEvaluation is AIDecision.Uncertain or AIDecision.NotReviewed));

            bool priceSuspicious = post.PriceEvaluation is PriceEvaluation.VeryHigh or PriceEvaluation.VeryLow;

            if (anyUncertain || priceSuspicious)
            {
                post.PendingStatus = PostPendingStatus.Pending;
                return;
            }

            post.PendingStatus = PostPendingStatus.Accepted;
        }

        // ============================================================
        // 07..12 no-ops
        // ============================================================

        private Task HandleSearchQueryUnderstanding(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleSearchSemanticRanking(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleSearchSimilarListings(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        private Task HandleRecoPersonalizedFeed(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleRecoRelatedPosts(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleRecoUserToUserMatch(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        private Task HandleNegotiationPriceSuggestion(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleNegotiationCounterOfferSuggestion(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        private Task HandleInsightsMarketTrends(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleInsightsDemandPrediction(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleInsightsUserBehaviorSummary(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        private Task HandleContractRiskFlags(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleContractClauseSuggestion(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;

        private Task HandleSupportAutoReplySuggestion(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleSupportTicketClassification(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
        private Task HandleSupportPriorityScoring(AiResultEnvelope env, CancellationToken ct) => Task.CompletedTask;
    }
}
