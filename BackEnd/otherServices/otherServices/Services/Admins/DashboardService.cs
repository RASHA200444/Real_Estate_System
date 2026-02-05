using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using otherServices.Repositories;
using otherServices.Services.Interfaces;
using otherServices.Models.Enums;
using otherServices.Models;
using Microsoft.EntityFrameworkCore;

namespace otherServices.Services.Admins
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private const string CacheKey = "dashboard_stats";

        public DashboardService(IUnitOfWork uow, IMemoryCache cache, ILogger<DashboardService> logger)
        {
            _uow = uow;
            _cache = cache;
            _logger = logger;
        }

        public async Task<object> GetDashboardAsync()
        {
            if (_cache.TryGetValue(CacheKey, out object cachedStats))
            {
                _logger.LogInformation("📦 Dashboard stats retrieved from cache at {Time}", DateTime.UtcNow);
                return cachedStats;
            }

            var sw = Stopwatch.StartNew();
            _logger.LogInformation("⏳ Calculating dashboard statistics...");

            var today = DateTime.UtcNow.Date;
            var oneMonthAgo = today.AddDays(-30);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            #region Users
            var totalUsers = await _uow.Users.CountAsync();
            var landlords = await _uow.Landlords.CountAsync(u => u.PendingStatus != PendingStatus.Blocked);
            var tenants = await _uow.Admins.CountAsync();
            var admins = await _uow.Users.CountAsync(u => u.RoleName == UserRole.Admin);
            var newUsersLastMonth = await _uow.Users.CountAsync(u => u.CreatedAt >= oneMonthAgo);
            var waitingLandlords = await _uow.Landlords.CountAsync(u => u.PendingStatus == PendingStatus.Pending);
            #endregion

            #region Posts & Moderation
            var postStats = await _uow.Posts
                .GetAllQueryable()
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Waiting = g.Count(p => p.PendingStatus == PostPendingStatus.Pending),
                    Refused = g.Count(p => p.PendingStatus == PostPendingStatus.Refused),
                    Accepted = g.Count(p => p.PendingStatus == PostPendingStatus.Accepted),
                    Sold = g.Count(p => p.Status == PropertyStatus.Sold),
                    Available = g.Count(p => p.Status == PropertyStatus.Available && p.PendingStatus != PostPendingStatus.Pending),
                    NewLastMonth = g.Count(p => p.CreatedAt >= oneMonthAgo),

                    Fraudulent = g.Count(p => p.PostDocPathEvaluation == AIDecision.Fraudulent),
                    UncertainFile = g.Count(p => p.PostDocPathEvaluation == AIDecision.Uncertain),

                    VeryLowPrices = g.Count(p => p.PriceEvaluation == PriceEvaluation.VeryLow),
                    LowPrices = g.Count(p => p.PriceEvaluation == PriceEvaluation.Low),
                    AcceptablePrices = g.Count(p => p.PriceEvaluation == PriceEvaluation.Acceptable),
                    HighPrices = g.Count(p => p.PriceEvaluation == PriceEvaluation.High),
                    VeryHighPrices = g.Count(p => p.PriceEvaluation == PriceEvaluation.VeryHigh),
                    AbnormalPrices = g.Count(p => p.PriceEvaluation == PriceEvaluation.VeryLow || p.PriceEvaluation == PriceEvaluation.VeryHigh),
                    UncertainPrice = g.Count(p => p.PriceEvaluation != PriceEvaluation.Acceptable)
                })
                .FirstOrDefaultAsync();

            // Null-safe
            postStats ??= new
            {
                Total = 0,
                Waiting = 0,
                Refused = 0,
                Accepted = 0,
                Sold = 0,
                Available = 0,
                NewLastMonth = 0,
                Fraudulent = 0,
                UncertainFile = 0,
                VeryLowPrices = 0,
                LowPrices = 0,
                AcceptablePrices = 0,
                HighPrices = 0,
                VeryHighPrices = 0,
                AbnormalPrices = 0,
                UncertainPrice = 0
            };
            #endregion

            #region Proposals
            var proposalStats = await _uow.Proposals
                .GetAllQueryable()
                .GroupBy(p => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Approved = g.Count(p => p.ProposalStatus == ProposalStatus.Approved),
                    Waiting = g.Count(p => p.ProposalStatus == ProposalStatus.Waiting),
                    Rejected = g.Count(p => p.ProposalStatus == ProposalStatus.Rejected),
                    PostsWithProposals = g.Select(p => p.PostId).Distinct().Count()
                })
                .FirstOrDefaultAsync();

            proposalStats ??= new
            {
                Total = 0,
                Approved = 0,
                Waiting = 0,
                Rejected = 0,
                PostsWithProposals = 0
            };

            double postsToProposalsPercentage = postStats.Total > 0
                ? ((double)proposalStats.PostsWithProposals / postStats.Total) * 100
                : 0;
            #endregion

            #region Transactions & Revenue
            var transactionStats = await _uow.Transactions
                .GetAllQueryable()
                .GroupBy(t => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Pending = g.Count(t => t.State == TransactionState.Pending),
                    Succeeded = g.Count(t => t.State == TransactionState.Succeeded),
                    Failed = g.Count(t => t.State == TransactionState.Failed),
                    AwaitingSignatures = g.Count(t => t.State == TransactionState.AwaitingSignatures),

                    GrossRevenue = g.Where(t => t.State == TransactionState.Succeeded).Sum(t => t.Amount),
                    InstallmentPaymentRevenue = g.Where(t => t.State == TransactionState.Succeeded && t.Kind == TransactionKind.InstallmentPayment).Sum(t => t.Amount),
                    SaleInstallmentRevenue = g.Where(t => t.State == TransactionState.Succeeded && t.Kind == TransactionKind.SaleInstallment).Sum(t => t.Amount),
                    SubscriptionRevenue = g.Where(t => t.State == TransactionState.Succeeded && t.Kind == TransactionKind.Subscription).Sum(t => t.Amount),
                    SaleCashRevenue = g.Where(t => t.State == TransactionState.Succeeded && t.Kind == TransactionKind.SaleCash).Sum(t => t.Amount),
                    RentRevenue = g.Where(t => t.State == TransactionState.Succeeded && t.Kind == TransactionKind.Rent).Sum(t => t.Amount),
                    FeeAmount = g.Where(t => t.State == TransactionState.Succeeded).Sum(t => t.FeeAmount),

                    MonthlySubscriptionRevenue = g.Where(t => t.State == TransactionState.Succeeded && t.Kind == TransactionKind.Subscription && t.CreatedAt >= thisMonth).Sum(t => t.Amount),
                    ActiveSubscriptionCount = g.Count(t => t.State == TransactionState.Succeeded &&
                                                           t.Kind == TransactionKind.Subscription &&
                                                           t.UserSubscription != null &&
                                                           t.UserSubscription.Status == SubscriptionStatus.Active &&
                                                           t.UserSubscription.EndDate >= today)
                })
                .FirstOrDefaultAsync();

            transactionStats ??= new
            {
                Total = 0,
                Pending = 0,
                Succeeded = 0,
                Failed = 0,
                AwaitingSignatures = 0,
                GrossRevenue = 0m,
                InstallmentPaymentRevenue = 0m,
                SaleInstallmentRevenue = 0m,
                SubscriptionRevenue = 0m,
                SaleCashRevenue = 0m,
                RentRevenue = 0m,
                FeeAmount = 0m,
                MonthlySubscriptionRevenue = 0m,
                ActiveSubscriptionCount = 0
            };
            #endregion

            #region Payments
            var paymentStats = await _uow.PaymentPlans
                .GetAllQueryable()
                .GroupBy(p => 1)
                .Select(g => new
                {
                    TotalPlans = g.Count(),
                    ActivePlans = g.Count(p => p.Status == PlanStatus.Active),
                    CompletedPlans = g.Count(p => p.Status == PlanStatus.Completed),
                    CancelledPlans = g.Count(p => p.Status == PlanStatus.Cancelled),
                })
                .FirstOrDefaultAsync();

            paymentStats ??= new
            {
                TotalPlans = 0,
                ActivePlans = 0,
                CompletedPlans = 0,
                CancelledPlans = 0
            };

            var overdueInstallments = await _uow.PaymentSchedules.CountAsync(s =>
                s.DueDate < today && !s.IsPaid
            );

            var totalDueInPeriod = await _uow.PaymentSchedules.CountAsync(s =>
                s.DueDate >= oneMonthAgo && s.DueDate <= today
            );

            var paidOnTime = await _uow.PaymentSchedules.CountAsync(s =>
                s.DueDate >= oneMonthAgo &&
                s.DueDate <= today &&
                s.IsPaid &&
                s.PaidAt != null &&
                s.PaidAt.Value.Date <= s.DueDate
            );

            double onTimeRate = totalDueInPeriod == 0 ? 0 : (double)paidOnTime / totalDueInPeriod * 100;
            #endregion

            #region Pro Subscribers
            var currentProSubscribers = await _uow.UserSubscriptions.CountAsync(s =>
                s.Status == SubscriptionStatus.Active &&
                s.EndDate >= today
            );

            var newSubscriptions = await _uow.UserSubscriptions.CountAsync(s => s.CreatedAt >= thisMonth);
            #endregion

            #region Complaints
            var complaintStats = await _uow.Complaints
                .GetAllQueryable()
                .Where(c => c.CreatedAt >= oneMonthAgo && c.CreatedAt <= today)
                .GroupBy(c => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Spam = g.Count(c => c.Type == ComplaintType.Spam),
                    Harassment = g.Count(c => c.Type == ComplaintType.Harassment),
                    Fraud = g.Count(c => c.Type == ComplaintType.Fraud),
                    Other = g.Count(c => c.Type == ComplaintType.Other),

                    Pending = g.Count(c => (c.Status ?? ComplaintStatus.Pending) == ComplaintStatus.Pending),
                    ActionTaken = g.Count(c => c.Status == ComplaintStatus.ActionTaken),
                    Rejected = g.Count(c => c.Status == ComplaintStatus.Rejected)
                })
                .FirstOrDefaultAsync();

            complaintStats ??= new
            {
                Total = 0,
                Spam = 0,
                Harassment = 0,
                Fraud = 0,
                Other = 0,
                Pending = 0,
                ActionTaken = 0,
                Rejected = 0
            };
            #endregion

            #region Final Stats Object
            var stats = new
            {
                Users = new
                {
                    TotalUsers = totalUsers,
                    Landlords = landlords,
                    Tenants = tenants,
                    Admins = admins,
                    NewUsersLastMonth = newUsersLastMonth,
                    WaitingLandlords = waitingLandlords
                },
                Posts = postStats,
                Proposals = new
                {
                    proposalStats.Total,
                    proposalStats.Approved,
                    proposalStats.Waiting,
                    proposalStats.Rejected,
                    PostsWithProposals = proposalStats.PostsWithProposals,
                    PostsToProposalsPercentage = postsToProposalsPercentage
                },
                Transactions = transactionStats,
                Payments = new
                {
                    paymentStats.TotalPlans,
                    paymentStats.ActivePlans,
                    paymentStats.CompletedPlans,
                    paymentStats.CancelledPlans,
                    OverdueInstallments = overdueInstallments,
                    OnTimeRate = onTimeRate
                },
                Subscriptions = new
                {
                    CurrentProSubscribers = currentProSubscribers,
                    NewThisMonth = newSubscriptions,
                    MonthlyRevenue = transactionStats.MonthlySubscriptionRevenue,
                    ActiveSubscriptionCount = transactionStats.ActiveSubscriptionCount
                },
                Complaints = complaintStats
            };
            #endregion

            _cache.Set(CacheKey, stats, TimeSpan.FromMinutes(2));

            sw.Stop();
            _logger.LogInformation("✅ Dashboard stats calculated in {Elapsed} ms", sw.ElapsedMilliseconds);

            return stats;
        }

        public void InvalidateDashboardCache()
        {
            _cache.Remove(CacheKey);
            _logger.LogInformation("🧹 Dashboard cache invalidated at {Time}", DateTime.UtcNow);
        }
    }
}
