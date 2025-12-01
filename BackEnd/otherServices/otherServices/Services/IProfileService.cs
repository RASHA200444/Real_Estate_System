using otherServices.Models.DTOs;
namespace otherServices.Services
{
    public interface IProfileService
    {
        Task<MyProfileDto> GetMyProfileAsync(int userId);
        Task<UserProfileDto> GetUserProfileAsync(int userId);
        Task UpdateMyProfileAsync(int userId, UpdateProfileDto dto);
        Task UpdatePasswordAsync(int userId, UpdatePasswordDto dto);
    }
}
