using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments
{
    public interface IMockBankCardVault
    {
        Task<long> SeedBankCardAsync(SeedBankCardRequestDto dto);
        Task<string> IssueTokenAsync(string cardNumber, int expiryMonth, int expiryYear, string cvv);

        Task<ChargeCardResponseDto> ChargeAsync(string token, string cvv, decimal amount);

        // ✅ NEW
        Task<(bool Success, string Message)> VerifyCvvAsync(string token, string cvv);

        // ✅ NEW: payer -> (admin fee + payee net)
        Task<TransferWithFeeResponseDto> TransferWithFeeAsync(
            string payerToken,
            string cvv,
            decimal amount,
            string payeeToken,
            string adminToken,
            decimal feeAmount
        );
    }
}
