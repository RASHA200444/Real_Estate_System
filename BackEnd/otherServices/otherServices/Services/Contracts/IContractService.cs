using otherServices.Models.Enums;

namespace otherServices.Services.Contracts
{
    public interface IContractService
    {
        Task<long> CreateDraftAsync(long postId, long tenantId, long landlordUserId, long proposalId, ContractType type, object snapshot);
        Task<object> SignAsync(long contractId, long signerUserId, SignerRole role, string? ip, string? userAgent);
        Task<object> VerifyAsync(long contractId);
        Task<object> GetContractAsync(long contractId);



    }
}
