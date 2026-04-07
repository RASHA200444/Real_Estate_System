using otherServices.Models.DTOs;

namespace otherServices.Services.Interfaces
{
    public interface IAdminBySysService
    {
        Task<AdminDto> CreateAdminAsync(CreateAdminDto dto);
        Task<(bool Success, string Message)> DeleteAdminAsync(long userId);
        //Task CreateAdminAsync(CreateAdminDto dto);
        //Task DeleteAdminAsync(long userId);
        Task<IEnumerable<AdminDto>> GetAllAsync();
        Task<AdminDto?> GetByUserIdAsync(long userId);
        Task<(bool Success, string Message)> UpdateAdminAsync(long userId, UpdateAdminDto dto);
        //Task<AdminDto> UpdateAdminAsync(long userId, UpdateAdminDto dto);
    }
}
