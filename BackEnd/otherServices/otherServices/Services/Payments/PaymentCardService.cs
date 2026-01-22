using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services;

namespace otherServices.Services.Payments
{
    public class PaymentCardService : IPaymentCardService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMockGatewayService _gateway;
        private readonly IMockBankCardVault _vault;     // ✅ NEW
        private readonly IEncryptionService _enc;

        public PaymentCardService(IUnitOfWork uow, IMockGatewayService gateway, IMockBankCardVault vault, IEncryptionService enc)
        {
            _uow = uow;
            _gateway = gateway;
            _vault = vault;
            _enc = enc;
        }

        public async Task<TokenizeCardResponseDto> TokenizeAndSaveAsync(long userId, TokenizeCardRequestDto dto)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            // ✅ Validate format (Luhn/expiry/cvv length)
            _gateway.ValidateCard(dto.CardNumber, dto.ExpiryMonth, dto.ExpiryYear, dto.CVV);

            var cardType = _gateway.DetectCardType(dto.CardNumber);

            // ✅ Token comes from Bank Vault (يتأكد CVV + موجود في البنك)
            var token = await _vault.IssueTokenAsync(dto.CardNumber, dto.ExpiryMonth, dto.ExpiryYear, dto.CVV);

            var tokenEncrypted = _enc.Encrypt(token);
            var masked = _gateway.MaskFromCardNumber(dto.CardNumber);

            // ✅ Duplicate check (masked + expiry + type)
            var existing = await _uow.PaymentCards.FindAsync(c =>
                c.UserId == userId &&
                c.MaskedCardNumber == masked &&
                c.ExpiryMonth == dto.ExpiryMonth &&
                c.ExpiryYear == dto.ExpiryYear &&
                c.CardType == cardType &&
                c.IsActive);

            if (existing.Any())
                throw new Exception("This card already exists for this user.");

            // ✅ first card => default
            var currentCards = await _uow.PaymentCards.FindAsync(c => c.UserId == userId && c.IsActive);
            bool makeDefault = !currentCards.Any();

            if (makeDefault)
                foreach (var c in currentCards) c.IsDefault = false;

            var entity = new PaymentCard
            {
                UserId = userId,
                CardTokenEncrypted = tokenEncrypted,
                MaskedCardNumber = masked,
                CardType = cardType,
                ExpiryMonth = dto.ExpiryMonth,
                ExpiryYear = dto.ExpiryYear,
                IsDefault = makeDefault,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.PaymentCards.AddAsync(entity);
            await _uow.CompleteAsync();

            return new TokenizeCardResponseDto
            {
                PaymentCardId = entity.PaymentCardId,
                MaskedCardNumber = entity.MaskedCardNumber,
                CardType = entity.CardType,
                ExpiryMonth = entity.ExpiryMonth,
                ExpiryYear = entity.ExpiryYear,
                IsDefault = entity.IsDefault
            };
        }

        public async Task<IEnumerable<PaymentCardDto>> GetAllAsync(long userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var cards = await _uow.PaymentCards.FindAsync(c => c.UserId == userId);

            return cards
                .OrderByDescending(c => c.IsDefault)
                .ThenByDescending(c => c.CreatedAt)
                .Select(c => new PaymentCardDto
                {
                    PaymentCardId = c.PaymentCardId,
                    MaskedCardNumber = c.MaskedCardNumber,
                    CardType = c.CardType,
                    ExpiryMonth = c.ExpiryMonth,
                    ExpiryYear = c.ExpiryYear,
                    IsDefault = c.IsDefault,
                    IsActive = c.IsActive
                })
                .ToList();
        }

        public async Task<PaymentCardDto> SetDefaultAsync(long userId, long paymentCardId)
        {
            var card = await _uow.PaymentCards.GetByIdAsync(paymentCardId);
            if (card == null) throw new KeyNotFoundException("Card not found.");
            if (card.UserId != userId) throw new UnauthorizedAccessException("You do not own this card.");
            if (!card.IsActive) throw new Exception("Card is not active.");

            var all = await _uow.PaymentCards.FindAsync(c => c.UserId == userId && c.IsActive);
            foreach (var c in all) c.IsDefault = false;

            card.IsDefault = true;
            _uow.PaymentCards.Update(card);
            await _uow.CompleteAsync();

            return new PaymentCardDto
            {
                PaymentCardId = card.PaymentCardId,
                MaskedCardNumber = card.MaskedCardNumber,
                CardType = card.CardType,
                ExpiryMonth = card.ExpiryMonth,
                ExpiryYear = card.ExpiryYear,
                IsDefault = card.IsDefault,
                IsActive = card.IsActive
            };
        }

        public async Task<bool> DeleteAsync(long userId, long paymentCardId)
        {
            var card = await _uow.PaymentCards.GetByIdAsync(paymentCardId);
            if (card == null) throw new KeyNotFoundException("Card not found.");
            if (card.UserId != userId) throw new UnauthorizedAccessException("You do not own this card.");

            await EnsureNoActivePlansAsync(paymentCardId);

            bool wasDefault = card.IsDefault;

            // ✅ قبل ما تمسح: شيل الديفولت عنه
            if (wasDefault)
            {
                card.IsDefault = false;
                _uow.PaymentCards.Update(card);
                await _uow.CompleteAsync();
            }

            // ✅ حذف نهائي
            _uow.PaymentCards.Remove(card);   // لازم يكون عندك Remove في الريبو/الجنريك
            await _uow.CompleteAsync();

            // ✅ عيّن كارت بديل Default لو كان هو الديفولت
            await AssignAnotherDefaultIfNeededAsync(userId, wasDefault);

            return true;
        }



        public async Task<bool> DeactivateAsync(long userId, long paymentCardId)
        {
            var card = await _uow.PaymentCards.GetByIdAsync(paymentCardId);
            if (card == null) throw new KeyNotFoundException("Card not found.");
            if (card.UserId != userId) throw new UnauthorizedAccessException("You do not own this card.");

            await EnsureNoActivePlansAsync(paymentCardId);

            bool wasDefault = card.IsDefault;

            card.IsActive = false;
            card.IsDefault = false;

            _uow.PaymentCards.Update(card);
            await _uow.CompleteAsync();

            await AssignAnotherDefaultIfNeededAsync(userId, wasDefault);

            return true;
        }


        private async Task EnsureNoActivePlansAsync(long paymentCardId)
        {
            var activePlans = await _uow.PaymentPlans.FindAsync(p =>
                p.PaymentCardId == paymentCardId && p.Status == PlanStatus.Active);

            if (activePlans.Any())
                throw new Exception("Operation blocked: this card is linked to an active rent/installment plan.");
        }

        private async Task AssignAnotherDefaultIfNeededAsync(long userId, bool needAssign)
        {
            if (!needAssign) return;

            var activeCards = (await _uow.PaymentCards.FindAsync(c => c.UserId == userId && c.IsActive))
                .OrderByDescending(c => c.IsDefault)
                .ThenByDescending(c => c.CreatedAt)
                .ToList();

            if (!activeCards.Any()) return;

            if (!activeCards.Any(c => c.IsDefault))
            {
                activeCards[0].IsDefault = true;
                _uow.PaymentCards.Update(activeCards[0]);
                await _uow.CompleteAsync();
            }
        }

    }
}
