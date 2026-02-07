using otherServices.Models.DTOs.Ads;

namespace otherServices.Services.Ads
{
    public interface IAdvertisementService
    {
        Task<long> CreateAdAsync(long adminUserId, CreateAdDto dto);
        Task ToggleAdAsync(long adminUserId, long adId, bool isActive);

        Task<PopupAdDto?> GetPopupAdAsync(long userId);
        Task TrackClickAsync(long userId, long adId);

        // ✅ NEW: Admin queries
        Task<IReadOnlyList<AdminAdDto>> GetAllAdsAsync(long adminUserId);
        Task<IReadOnlyList<EligiblePostDto>> GetEligiblePostsAsync(long adminUserId);
    }
}
