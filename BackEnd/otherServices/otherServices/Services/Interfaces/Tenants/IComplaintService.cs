using otherServices.Models.DTOs.Complaints;

namespace otherServices.Services.Interfaces.Tenants
{
    public interface IComplaintService
    {
        Task CreateComplaintAsync(long ReporterUserId, ComplaintCreateDto dto);
        Task<IEnumerable<ComplaintDto>> GetComplaintsAsync();
        Task<ComplaintDetailsDto?> GetComplaintByIdAsync(int complaintId);
        Task BanUserAsync(int complaintId);
        Task SuspendUserAsync(int complaintId, int days);
        Task RefuseComplaintAsync(int complaintId);

    }
}
