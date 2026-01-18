using Microsoft.Extensions.Configuration;
using otherServices.Models;
using otherServices.Repositories;

namespace otherServices.Services.Payments.Helpers
{
    public class PaymentFlowHelpers : IPaymentFlowHelpers
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _cfg;

        public PaymentFlowHelpers(IUnitOfWork uow, IConfiguration cfg)
        {
            _uow = uow;
            _cfg = cfg;
        }

        public decimal GetFeePercent()
        {
            var p = _cfg.GetValue<decimal>("Payments:PlatformFeePercent");
            return p <= 0 ? 0 : p;
        }

        public long GetAdminUserId()
        {
            var id = _cfg.GetValue<long>("Payments:AdminUserId");
            return id > 0 ? id : 4; // fallback TEMP
        }

        public async Task<PaymentCard?> GetDefaultActiveCardAsync(long userId)
        {
            var def = await _uow.PaymentCards.FirstOrDefaultAsync(c =>
                c.UserId == userId && c.IsActive && c.IsDefault);

            if (def != null) return def;

            return await _uow.PaymentCards.FirstOrDefaultAsync(c =>
                c.UserId == userId && c.IsActive);
        }

        public async Task CreateNotificationAsync(long userId, string content)
        {
            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Content = content,
                ReadStatus = false,
                CreatedAt = DateTime.UtcNow
            });

            await _uow.CompleteAsync();
        }
    }
}
