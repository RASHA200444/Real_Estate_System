using otherServices.Models.DTOs;

namespace otherServices.Services.Interfaces
{
    public interface IAdminBySysService
    {
        Task<AdminDto> CreateAdminAsync(CreateAdminDto dto, int createdByAdminId);
        Task<IEnumerable<AdminDto>> GetAllAsync();
        Task<AdminDto?> GetByIdAsync(int adminId);
        Task<(bool Success, string Message)> UpdateAdminAsync(int adminId, UpdateAdminDto dto);
        Task<(bool Success, string Message)> DeleteAdminAsync(int adminId);
    }
}
