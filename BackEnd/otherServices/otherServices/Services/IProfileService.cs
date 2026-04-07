using otherServices.Models.DTOs;
namespace otherServices.Services
{
    public interface IProfileService
    {
        Task<MyProfileDto> GetMyProfileAsync(long userId);
        Task<UserProfileDto> GetUserProfileAsync(long userId);
        Task UpdateMyProfileAsync(long userId, UpdateProfileDto dto);
        Task UpdatePasswordAsync(long userId, UpdatePasswordDto dto);
        Task DeleteProfileAsync(long userId, string dto);
    }
}
