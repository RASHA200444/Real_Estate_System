using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments.Eligibility
{
    public interface IEligibilityService
    {
        Task<EligibilityResultDto> EvaluateInstallmentAsync(long tenantUserId, long proposalId, EligibilityFormRequestDto dto);
        Task<EligibilityResultDto> EvaluateRentAsync(long tenantUserId, long proposalId, EligibilityFormRequestDto dto);

        Task<object> GetEligibilitySnapshotAsync(long tenantUserId, long proposalId);
    }
}
