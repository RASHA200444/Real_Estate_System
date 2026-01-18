using otherServices.Models;

namespace otherServices.Services.Payments.Helpers
{
    public interface IPaymentFlowHelpers
    {
        decimal GetFeePercent();
        long GetAdminUserId();
        Task<PaymentCard?> GetDefaultActiveCardAsync(long userId);
        Task CreateNotificationAsync(long userId, string content);
    }
}
