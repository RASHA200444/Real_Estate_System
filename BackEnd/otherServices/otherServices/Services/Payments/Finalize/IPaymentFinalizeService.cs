using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments.Finalize
{
    public interface IPaymentFinalizeService
    {
        Task<object> FinalizeAsync(long requesterUserId, FinalizePaymentDto dto);
    }
}
