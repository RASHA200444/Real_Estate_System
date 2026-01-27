using System.Text.Json;
using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;

namespace otherServices.Services.Payments.Eligibility
{
    public class EligibilityService : IEligibilityService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAiEligibilityClient _ai; // placeholder (no real calls now)

        public EligibilityService(IUnitOfWork uow, IAiEligibilityClient ai)
        {
            _uow = uow;
            _ai = ai;
        }

        public async Task<EligibilityResultDto> EvaluateInstallmentAsync(long tenantUserId, long proposalId, EligibilityFormRequestDto dto)
        {
            var proposal = await _uow.Proposals.GetByIdAsync(proposalId);
            if (proposal == null) return Fail("Proposal not found");
            if (proposal.TenantId != tenantUserId) return Fail("You do not own this proposal");

            var post = await _uow.Posts.GetByIdAsync(proposal.PostId);
            if (post == null) return Fail("Post not found");

            if (post.Type != PropertyType.Sale)
                return Fail("EligibilityInstallment is for SALE installment only");

            // derive total price (auction uses offered)
            decimal total;
            if (post.IsAuction)
            {
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value <= 0)
                    return Fail("Auction requires Offeredprice in proposal");
                total = (decimal)proposal.Offeredprice.Value;
            }
            else
            {
                if (!post.Price.HasValue || post.Price.Value <= 0)
                    return Fail("Post price is missing");
                total = (decimal)post.Price.Value;
            }

            // compute required monthly/periodic payment
            var duration = proposal.InstallmentDurationMonths ?? 0;
            var interval = proposal.InstallmentIntervalMonths ?? 0;

            if (duration <= 0 || interval <= 0 || duration % interval != 0)
                return Fail("Proposal installment terms missing/invalid (DurationMonths / IntervalMonths)");

            var paymentsCount = duration / interval;
            if (paymentsCount <= 0) return Fail("Invalid installment plan");

            var perPayment = Math.Round(total / paymentsCount, 2);

            // Build JSON payload (saved for Kafka/AI later)
            var payload = new
            {
                Type = "Installment",
                ProposalId = proposal.ProposalId,
                PostId = post.PostId,
                TotalPrice = total,
                DurationMonths = duration,
                IntervalMonths = interval,
                RequiredPayment = perPayment,
                Applicant = new
                {
                    dto.MonthlyIncome,
                    dto.MonthlyExpenses,
                    dto.ExistingMonthlyDebt,
                    dto.HasStableJob,
                    dto.Dependents
                },
                ExtraAnswers = dto.ExtraAnswers
            };

            var json = JsonSerializer.Serialize(payload);

            // ✅ rule-based decision now
            var disposable = dto.MonthlyIncome - dto.MonthlyExpenses - dto.ExistingMonthlyDebt;
            var threshold = perPayment * 1.20m; // safety ratio

            AIInstallmentDecision decision;
            string reason;
            int score;

            if (dto.MonthlyIncome <= 0)
            {
                decision = AIInstallmentDecision.NotCertain;
                reason = "Missing/invalid income";
                score = 20;
            }
            else if (!dto.HasStableJob)
            {
                decision = AIInstallmentDecision.NotCertain;
                reason = "Unstable job";
                score = 40;
            }
            else if (disposable >= threshold)
            {
                decision = AIInstallmentDecision.Able;
                reason = $"Disposable income {disposable} >= required {threshold}";
                score = 85;
            }
            else
            {
                decision = AIInstallmentDecision.NotCertain;
                reason = $"Disposable income {disposable} < required {threshold}";
                score = 45;
            }

            // Save to Proposal (AI-ready)
            proposal.EligibilityAnswersJson = json;
            proposal.EligibilityScore = score;
            proposal.EligibilityReason = reason;
            proposal.EligibilityAssessedAt = DateTime.UtcNow;

            proposal.IsAble = decision;
            _uow.Proposals.Update(proposal);
            await _uow.CompleteAsync();

            return new EligibilityResultDto
            {
                Success = true,
                Message = "Eligibility evaluated (mock rules).",
                Score = score,
                Reason = reason,
                InstallmentDecision = (int)decision
            };
        }

        public async Task<EligibilityResultDto> EvaluateRentAsync(long tenantUserId, long proposalId, EligibilityFormRequestDto dto)
        {
            var proposal = await _uow.Proposals.GetByIdAsync(proposalId);
            if (proposal == null) return Fail("Proposal not found");
            if (proposal.TenantId != tenantUserId) return Fail("You do not own this proposal");

            var post = await _uow.Posts.GetByIdAsync(proposal.PostId);
            if (post == null) return Fail("Post not found");

            if (post.Type != PropertyType.Rent)
                return Fail("EligibilityRent is for RENT only");

            // monthly amount rules (auction uses offered)
            decimal monthly;
            if (post.IsAuction)
            {
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value <= 0)
                    return Fail("Auction requires Offeredprice in proposal");
                monthly = (decimal)proposal.Offeredprice.Value;
            }
            else
            {
                if (!post.Price.HasValue || post.Price.Value <= 0)
                    return Fail("Post price is missing");
                monthly = (decimal)post.Price.Value;
            }

            var payload = new
            {
                Type = "Rent",
                ProposalId = proposal.ProposalId,
                PostId = post.PostId,
                RequiredMonthlyRent = monthly,
                Applicant = new
                {
                    dto.MonthlyIncome,
                    dto.MonthlyExpenses,
                    dto.ExistingMonthlyDebt,
                    dto.HasStableJob,
                    dto.Dependents
                },
                ExtraAnswers = dto.ExtraAnswers
            };

            var json = JsonSerializer.Serialize(payload);

            var disposable = dto.MonthlyIncome - dto.MonthlyExpenses - dto.ExistingMonthlyDebt;
            var threshold = monthly * 1.20m;

            AIRentDecision decision;
            string reason;
            int score;

            if (dto.MonthlyIncome <= 0)
            {
                decision = AIRentDecision.NotCertain;
                reason = "Missing/invalid income";
                score = 20;
            }
            else if (!dto.HasStableJob)
            {
                decision = AIRentDecision.NotCertain;
                reason = "Unstable job";
                score = 40;
            }
            else if (disposable >= threshold)
            {
                decision = AIRentDecision.Able;
                reason = $"Disposable income {disposable} >= required {threshold}";
                score = 85;
            }
            else
            {
                decision = AIRentDecision.NotCertain;
                reason = $"Disposable income {disposable} < required {threshold}";
                score = 45;
            }

            proposal.EligibilityAnswersJson = json;
            proposal.EligibilityScore = score;
            proposal.EligibilityReason = reason;
            proposal.EligibilityAssessedAt = DateTime.UtcNow;

            proposal.RentIsAble = decision;
            _uow.Proposals.Update(proposal);
            await _uow.CompleteAsync();

            return new EligibilityResultDto
            {
                Success = true,
                Message = "Eligibility evaluated (mock rules).",
                Score = score,
                Reason = reason,
                RentDecision = (int)decision
            };
        }

        public async Task<object> GetEligibilitySnapshotAsync(long tenantUserId, long proposalId)
        {
            var proposal = await _uow.Proposals.GetByIdAsync(proposalId);
            if (proposal == null) return new { success = false, message = "Proposal not found" };
            if (proposal.TenantId != tenantUserId) return new { success = false, message = "You do not own this proposal" };

            return new
            {
                success = true,
                proposalId = proposal.ProposalId,
                installmentDecision = (int)proposal.IsAble,
                rentDecision = (int)proposal.RentIsAble,
                proposal.EligibilityScore,
                proposal.EligibilityReason,
                proposal.EligibilityAssessedAt,
                proposal.EligibilityAnswersJson
            };
        }

        private static EligibilityResultDto Fail(string msg)
            => new EligibilityResultDto { Success = false, Message = msg };
    }
}
