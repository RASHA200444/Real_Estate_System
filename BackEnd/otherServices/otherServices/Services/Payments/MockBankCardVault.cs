using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services;

namespace otherServices.Services.Payments
{
    public class MockBankCardVault : IMockBankCardVault
    {
        private readonly IUnitOfWork _uow;
        private readonly IEncryptionService _enc;

        private const string BankSecret = "BANK_INTERNAL_SECRET_CHANGE_LATER";

        public MockBankCardVault(IUnitOfWork uow, IEncryptionService enc)
        {
            _uow = uow;
            _enc = enc;
        }

        public async Task<long> SeedBankCardAsync(SeedBankCardRequestDto dto)
        {
            var sanitized = Sanitize(dto.CardNumber);
            if (!IsValidLuhn(sanitized)) throw new Exception("Invalid card number");

            var card = new BankCard
            {
                CardNumberEncrypted = _enc.Encrypt(sanitized),
                CvvHash = HashCvv(dto.CVV),
                CardType = DetectCardType(sanitized),
                ExpiryMonth = dto.ExpiryMonth,
                ExpiryYear = dto.ExpiryYear,
                Balance = dto.Balance,
                IsActive = true,
                FailedChargeCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.BankCards.AddAsync(card);
            await _uow.CompleteAsync();
            return card.BankCardId;
        }

        public async Task<string> IssueTokenAsync(string cardNumber, int expiryMonth, int expiryYear, string cvv)
        {
            var sanitized = Sanitize(cardNumber);

            var bankCards = await _uow.BankCards.GetAllAsync();
            BankCard? found = null;

            foreach (var c in bankCards)
            {
                var plain = _enc.Decrypt(c.CardNumberEncrypted);
                if (plain == sanitized && c.ExpiryMonth == expiryMonth && c.ExpiryYear == expiryYear && c.IsActive)
                {
                    found = c;
                    break;
                }
            }

            if (found == null) throw new Exception("Card not found in bank");

            if (found.CvvHash != HashCvv(cvv))
                throw new Exception("Invalid CVV");

            var token = GenerateToken(found.BankCardId, sanitized);

            var map = new BankTokenMap
            {
                BankCardId = found.BankCardId,
                Token = token,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.BankTokenMaps.AddAsync(map);
            await _uow.CompleteAsync();

            return token;
        }

        public async Task<ChargeCardResponseDto> ChargeAsync(string token, string cvv, decimal amount)
        {
            if (amount <= 0)
                return new ChargeCardResponseDto { Success = false, Message = "Invalid amount" };

            var map = await _uow.BankTokenMaps.FirstOrDefaultAsync(m => m.Token == token);

            if (map == null)
                return new ChargeCardResponseDto { Success = false, Message = "Invalid token" };

            var card = await _uow.BankCards.GetByIdAsync(map.BankCardId);
            if (card == null || !card.IsActive)
                return new ChargeCardResponseDto { Success = false, Message = "Card not active" };

            // ✅ MOCK ONLY: recurring payments won't have CVV
            if (cvv != "000" && card.CvvHash != HashCvv(cvv))
            {
                card.FailedChargeCount += 1;
                _uow.BankCards.Update(card);
                await _uow.CompleteAsync();

                return new ChargeCardResponseDto { Success = false, Message = "Invalid CVV" };
            }

            card.FailedChargeCount = 0;

            if (card.Balance < amount)
                return new ChargeCardResponseDto { Success = false, Message = "Insufficient balance" };

            card.Balance -= amount;
            _uow.BankCards.Update(card);
            await _uow.CompleteAsync();

            return new ChargeCardResponseDto
            {
                Success = true,
                Message = "Charged",
                RemainingBalance = card.Balance
            };
        }

        // =========================
        // ✅ NEW: CVV verification
        // =========================
        public async Task<(bool Success, string Message)> VerifyCvvAsync(string token, string cvv)
        {
            var map = await _uow.BankTokenMaps.FirstOrDefaultAsync(m => m.Token == token);
            if (map == null) return (false, "Invalid token");

            var card = await _uow.BankCards.GetByIdAsync(map.BankCardId);
            if (card == null || !card.IsActive) return (false, "Card not active");

            if (cvv != "000" && card.CvvHash != HashCvv(cvv))
                return (false, "Invalid CVV");

            return (true, "OK");
        }

        // ================================================
        // ✅ Transfer payer -> (admin fee + payee net)
        // ✅ FIXED: avoid nested transactions
        // ================================================
        public async Task<TransferWithFeeResponseDto> TransferWithFeeAsync(
            string payerToken,
            string cvv,
            decimal amount,
            string payeeToken,
            string adminToken,
            decimal feeAmount
        )
        {
            if (amount <= 0)
                return new TransferWithFeeResponseDto { Success = false, Message = "Invalid amount" };

            if (feeAmount < 0 || feeAmount > amount)
                return new TransferWithFeeResponseDto { Success = false, Message = "Invalid fee amount" };

            var net = amount - feeAmount;

            // Load token maps
            var payerMap = await _uow.BankTokenMaps.FirstOrDefaultAsync(m => m.Token == payerToken);
            if (payerMap == null)
                return new TransferWithFeeResponseDto { Success = false, Message = "Invalid payer token" };

            var payeeMap = await _uow.BankTokenMaps.FirstOrDefaultAsync(m => m.Token == payeeToken);
            if (payeeMap == null)
                return new TransferWithFeeResponseDto { Success = false, Message = "Invalid payee token" };

            var adminMap = await _uow.BankTokenMaps.FirstOrDefaultAsync(m => m.Token == adminToken);
            if (adminMap == null)
                return new TransferWithFeeResponseDto { Success = false, Message = "Invalid admin token" };

            // Load cards
            var payer = await _uow.BankCards.GetByIdAsync(payerMap.BankCardId);
            var payee = await _uow.BankCards.GetByIdAsync(payeeMap.BankCardId);
            var admin = await _uow.BankCards.GetByIdAsync(adminMap.BankCardId);

            if (payer == null || !payer.IsActive) return new TransferWithFeeResponseDto { Success = false, Message = "Payer card not active" };
            if (payee == null || !payee.IsActive) return new TransferWithFeeResponseDto { Success = false, Message = "Payee card not active" };
            if (admin == null || !admin.IsActive) return new TransferWithFeeResponseDto { Success = false, Message = "Admin card not active" };

            // Verify CVV (mock recurring uses "000")
            if (cvv != "000" && payer.CvvHash != HashCvv(cvv))
            {
                payer.FailedChargeCount += 1;
                _uow.BankCards.Update(payer);
                await _uow.CompleteAsync();

                return new TransferWithFeeResponseDto { Success = false, Message = "Invalid CVV" };
            }

            payer.FailedChargeCount = 0;

            if (payer.Balance < amount)
                return new TransferWithFeeResponseDto { Success = false, Message = "Insufficient balance" };

            // DbContext hook
            var db = TryGetDbContext();

            // ✅ If no DbContext -> fallback (no explicit transaction)
            if (db == null)
            {
                payer.Balance -= amount;
                admin.Balance += feeAmount;
                payee.Balance += net;

                _uow.BankCards.Update(payer);
                _uow.BankCards.Update(admin);
                _uow.BankCards.Update(payee);
                await _uow.CompleteAsync();

                return new TransferWithFeeResponseDto
                {
                    Success = true,
                    Message = "Transferred",
                    PayerRemainingBalance = payer.Balance,
                    PayeeBalance = payee.Balance,
                    AdminBalance = admin.Balance,
                    FeeAmount = feeAmount,
                    NetToPayee = net
                };
            }

            // ✅ IMPORTANT: if caller already started a transaction (Finalize Service),
            // do NOT start a new one
            if (db.Database.CurrentTransaction != null)
            {
                payer.Balance -= amount;
                admin.Balance += feeAmount;
                payee.Balance += net;

                _uow.BankCards.Update(payer);
                _uow.BankCards.Update(admin);
                _uow.BankCards.Update(payee);
                await _uow.CompleteAsync();

                return new TransferWithFeeResponseDto
                {
                    Success = true,
                    Message = "Transferred",
                    PayerRemainingBalance = payer.Balance,
                    PayeeBalance = payee.Balance,
                    AdminBalance = admin.Balance,
                    FeeAmount = feeAmount,
                    NetToPayee = net
                };
            }

            // ✅ Standalone calls: open transaction here
            await using var trx = await db.Database.BeginTransactionAsync();
            try
            {
                payer.Balance -= amount;
                admin.Balance += feeAmount;
                payee.Balance += net;

                _uow.BankCards.Update(payer);
                _uow.BankCards.Update(admin);
                _uow.BankCards.Update(payee);

                await _uow.CompleteAsync();
                await trx.CommitAsync();

                return new TransferWithFeeResponseDto
                {
                    Success = true,
                    Message = "Transferred",
                    PayerRemainingBalance = payer.Balance,
                    PayeeBalance = payee.Balance,
                    AdminBalance = admin.Balance,
                    FeeAmount = feeAmount,
                    NetToPayee = net
                };
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                return new TransferWithFeeResponseDto
                {
                    Success = false,
                    Message = ex.InnerException?.Message ?? ex.Message,
                    FeeAmount = feeAmount,
                    NetToPayee = net
                };
            }
        }

        // ================= Helpers =================
        private string Sanitize(string input) => new string((input ?? "").Where(char.IsDigit).ToArray());

        private string HashCvv(string cvv)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(BankSecret));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(cvv.Trim()));
            return Convert.ToBase64String(bytes);
        }

        private string GenerateToken(long bankCardId, string cardNumber)
        {
            var last4 = cardNumber.Length >= 4 ? cardNumber[^4..] : "0000";
            var raw = $"{bankCardId}|{last4}|{Guid.NewGuid()}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(BankSecret));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        private CardType DetectCardType(string number)
        {
            if (number.StartsWith("4")) return CardType.Visa;
            if (number.StartsWith("5")) return CardType.MasterCard;
            if (number.StartsWith("34") || number.StartsWith("37")) return CardType.AmericanExpress;
            return CardType.Unknown;
        }

        private bool IsValidLuhn(string cardNumber)
        {
            int sum = 0; bool alt = false;
            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int n = cardNumber[i] - '0';
                if (alt) { n *= 2; if (n > 9) n -= 9; }
                sum += n; alt = !alt;
            }
            return sum % 10 == 0;
        }

        // ✅ DbContext transaction hook (works because UnitOfWork exposes Context)
        private AppDbContext2? TryGetDbContext()
        {
            return (_uow as UnitOfWork)?.Context;
        }
    }
}
