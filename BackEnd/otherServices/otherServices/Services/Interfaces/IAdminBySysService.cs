using otherServices.Models.DTOs;

namespace otherServices.Services.Interfaces
{
    public interface IAdminBySysService
    {
        Task CreateAdminAsync(CreateAdminDto dto);
        Task DeleteAdminAsync(long userId);
        Task<IEnumerable<AdminDto>> GetAllAsync();
        Task<AdminDto?> GetByUserIdAsync(long userId);
        Task<AdminDto> UpdateAdminAsync(long userId, UpdateAdminDto dto);
    }
}
