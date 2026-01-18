using otherServices.Models.DTOs;

namespace otherServices.Services.Interfaces
{
    public interface IAdminBySysService
    {
        Task<AdminDto> CreateAdminAsync(CreateAdminDto dto);
        Task<IEnumerable<AdminDto>> GetAllAsync();
        Task<AdminDto?> GetByUserIdAsync(long userId);
        Task<(bool Success, string Message)> UpdateAdminAsync(long userId, UpdateAdminDto dto);
        Task<(bool Success, string Message)> DeleteAdminAsync(long userId);
    }
}
