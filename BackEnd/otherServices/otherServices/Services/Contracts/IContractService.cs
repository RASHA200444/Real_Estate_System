using otherServices.Models.DTOs.Contracts;
using otherServices.Models.Enums;

namespace otherServices.Services.Contracts
{
    public interface IContractService
    {
        Task<long> CreateDraftAsync(
            long postId,
            long tenantId,
            long landlordUserId,
            long proposalId,
            ContractType type,
            object snapshot);

        // ✅ NEW: cards list (for MyContracts page)
        Task<object> GetMyContractsAsync(long requesterUserId);

        // ✅ NEW: details DTO (for ContractDetails page)
        Task<ContractDetailsResponseDto> GetContractForUserAsync(long contractId, long requesterUserId);

        Task<object> SignAsync(long contractId, long signerUserId, SignerRole role, string? ip, string? userAgent);
        Task<object> VerifyAsync(long contractId);
    }
}
