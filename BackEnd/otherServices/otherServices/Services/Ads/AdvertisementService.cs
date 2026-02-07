using Microsoft.EntityFrameworkCore;
using otherServices.Models;
using otherServices.Models.DTOs.Ads;
using otherServices.Models.Enums;
using otherServices.Repositories;

namespace otherServices.Services.Ads
{
    public class AdvertisementService : IAdvertisementService
    {
        private readonly IUnitOfWork _uow;

        public AdvertisementService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        private static int TodayKeyUtc() => int.Parse(DateTime.UtcNow.ToString("yyyyMMdd"));

        private async Task<User> GetUserOrThrow(long userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");
            return user;
        }

        private async Task EnsureAdmin(long adminUserId)
        {
            var admin = await GetUserOrThrow(adminUserId);
            if (admin.RoleName != UserRole.Admin)
                throw new Exception("Forbidden: Admin only");
        }

        // ✅ UPDATED: Pro = Landlord.IsPro AND Active Subscription AND Plan.IsActive
        private async Task<bool> IsPostOwnerProAsync(Post post)
        {
            // 1) landlord exists + IsPro flag
            var landlords = await _uow.Landlords.FindAsync(l => l.LandlordId == post.LandlordId);
            var landlord = landlords.FirstOrDefault();
            if (landlord == null) return false;

            if (!landlord.IsPro) return false;

            // 2) active subscription for landlord's user
            var now = DateTime.UtcNow;

            var activeSub = await _uow.UserSubscriptions.GetAllQueryable()
                .AsNoTracking()
                .Where(s =>
                    s.UserId == landlord.UserId &&
                    s.Status == SubscriptionStatus.Active &&
                    s.StartDate <= now &&
                    s.EndDate > now)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

            if (activeSub == null) return false;

            // 3) plan must be active
            var plan = await _uow.SubscriptionPlans.GetByIdAsync(activeSub.SubscriptionPlanId);
            if (plan == null) return false;

            if (!plan.IsActive) return false;

            return true;
        }

        // =========================
        // Admin Queries
        // =========================

        public async Task<IReadOnlyList<AdminAdDto>> GetAllAdsAsync(long adminUserId)
        {
            await EnsureAdmin(adminUserId);

            var ads = await _uow.Advertisements.GetAllQueryable()
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return ads.Select(a => new AdminAdDto
            {
                AdId = a.AdvertisementId,
                PostId = a.PostId,
                Title = a.Title,
                Body = a.Body,
                IsActive = a.IsActive,
                Priority = a.Priority,
                MaxImpressionsPerUserPerDay = a.MaxImpressionsPerUserPerDay,
                StartAt = a.StartAt,
                EndAt = a.EndAt,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task<IReadOnlyList<EligiblePostDto>> GetEligiblePostsAsync(long adminUserId)
        {
            await EnsureAdmin(adminUserId);

            // base filter: accepted + not sold
            var posts = await _uow.Posts.GetAllQueryable()
                .AsNoTracking()
                .Where(p => p.PendingStatus == PostPendingStatus.Accepted &&
                            p.Status != PropertyStatus.Sold)
                .OrderByDescending(p => p.CreatedAt)
                .Take(200)
                .ToListAsync();

            var result = new List<EligiblePostDto>();

            foreach (var post in posts)
            {
                // ✅ Pro checks (IsPro + Active Subscription + Plan Active)
                var isPro = await IsPostOwnerProAsync(post);
                if (!isPro) continue;

                // landlord + landlord userName (optional)
                var landlord = await _uow.Landlords.GetByIdAsync(post.LandlordId);
                if (landlord == null) continue;

                var landlordUser = await _uow.Users.GetByIdAsync(landlord.UserId);

                result.Add(new EligiblePostDto
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    Price = post.Price,
                    IsAuction = post.IsAuction,

                    Type = post.Type,
                    Status = post.Status,
                    PendingStatus = post.PendingStatus,

                    LandlordId = post.LandlordId,
                    LandlordUserId = landlord.UserId,
                    LandlordName = landlordUser?.UserName,
                    CreatedAt = post.CreatedAt
                });
            }

            return result;
        }

        // =========================
        // Admin Create/Toggle
        // =========================

        public async Task<long> CreateAdAsync(long adminUserId, CreateAdDto dto)
        {
            await EnsureAdmin(adminUserId);

            var post = await _uow.Posts.GetByIdAsync(dto.PostId);
            if (post == null) throw new Exception("Post not found");

            // rules
            if (post.PendingStatus != PostPendingStatus.Accepted)
                throw new Exception("Post must be accepted by admin first");

            if (post.Status == PropertyStatus.Sold)
                throw new Exception("Cannot advertise a sold property");

            // ✅ Pro checks (IsPro + Subscription Active + Plan IsActive)
            var isPro = await IsPostOwnerProAsync(post);
            if (!isPro)
                throw new Exception("Only Pro users with an ACTIVE subscription plan can be advertised");

            var startAt = dto.StartAt ?? DateTime.UtcNow;
            if (dto.EndAt.HasValue && dto.EndAt.Value <= startAt)
                throw new Exception("EndAt must be after StartAt");

            var max = dto.MaxImpressionsPerUserPerDay <= 0 ? 1 : dto.MaxImpressionsPerUserPerDay;

            var ad = new Advertisement
            {
                PostId = dto.PostId,
                CreatedByAdminUserId = adminUserId,
                Title = dto.Title,
                Body = dto.Body,
                Priority = dto.Priority,
                MaxImpressionsPerUserPerDay = max,
                StartAt = startAt,
                EndAt = dto.EndAt,
                IsActive = true
            };

            await _uow.Advertisements.AddAsync(ad);
            await _uow.CompleteAsync();

            return ad.AdvertisementId;
        }

        public async Task ToggleAdAsync(long adminUserId, long adId, bool isActive)
        {
            await EnsureAdmin(adminUserId);

            var ad = await _uow.Advertisements.GetByIdAsync(adId);
            if (ad == null) throw new Exception("Ad not found");

            ad.IsActive = isActive;
            _uow.Advertisements.Update(ad);

            await _uow.CompleteAsync();
        }

        // =========================
        // Popup / Click Tracking
        // =========================

        public async Task<PopupAdDto?> GetPopupAdAsync(long userId)
        {
            await GetUserOrThrow(userId);

            var now = DateTime.UtcNow;
            var dateKey = TodayKeyUtc();

            // impressions today grouped
            var impressionsToday = await _uow.AdImpressions.GetAllQueryable()
                .AsNoTracking()
                .Where(i => i.UserId == userId && i.DateKey == dateKey)
                .GroupBy(i => i.AdvertisementId)
                .Select(g => new { AdId = g.Key, Count = g.Count() })
                .ToListAsync();

            // active ads + time window
            var ads = await _uow.Advertisements.GetAllQueryable()
                .AsNoTracking()
                .Where(a => a.IsActive &&
                            a.StartAt <= now &&
                            (a.EndAt == null || a.EndAt >= now))
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .Take(50)
                .ToListAsync();

            Advertisement? chosen = null;

            foreach (var ad in ads)
            {
                var post = await _uow.Posts.GetByIdAsync(ad.PostId);
                if (post == null) continue;

                if (post.PendingStatus != PostPendingStatus.Accepted) continue;
                if (post.Status == PropertyStatus.Sold) continue;

                // ✅ Pro checks
                var ownerPro = await IsPostOwnerProAsync(post);
                if (!ownerPro) continue;

                var row = impressionsToday.FirstOrDefault(x => x.AdId == ad.AdvertisementId);
                var seen = row?.Count ?? 0;

                if (seen < ad.MaxImpressionsPerUserPerDay)
                {
                    chosen = ad;
                    break;
                }
            }

            if (chosen == null) return null;

            // log impression (once chosen)
            var imp = new AdImpression
            {
                AdvertisementId = chosen.AdvertisementId,
                UserId = userId,
                SeenAt = now,
                DateKey = dateKey
            };

            await _uow.AdImpressions.AddAsync(imp);
            await _uow.CompleteAsync();

            return new PopupAdDto
            {
                AdId = chosen.AdvertisementId,
                PostId = chosen.PostId,
                Title = chosen.Title,
                Body = chosen.Body,

                // ✅ IMPORTANT: return FRONT route, not API route
                NavigateTo = $"/post/{chosen.PostId}",

                ExpiresAt = chosen.EndAt
            };
        }

        public async Task TrackClickAsync(long userId, long adId)
        {
            await GetUserOrThrow(userId);

            var now = DateTime.UtcNow;
            var dateKey = TodayKeyUtc();

            var last = await _uow.AdImpressions.GetAllQueryable()
                .Where(i => i.UserId == userId && i.AdvertisementId == adId && i.DateKey == dateKey)
                .OrderByDescending(i => i.SeenAt)
                .FirstOrDefaultAsync();

            if (last == null)
            {
                await _uow.AdImpressions.AddAsync(new AdImpression
                {
                    AdvertisementId = adId,
                    UserId = userId,
                    SeenAt = now,
                    ClickedAt = now,
                    DateKey = dateKey
                });
                await _uow.CompleteAsync();
                return;
            }

            last.ClickedAt = now;
            await _uow.CompleteAsync();
        }
    }
}
