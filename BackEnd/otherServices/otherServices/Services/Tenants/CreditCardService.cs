//using Microsoft.Extensions.Caching.Memory;
//using otherServices.Models;
//using otherServices.Models.DTOs.CreditCards;
//using otherServices.Models.Enums;
//using otherServices.Repositories;
//using otherServices.Services.Interfaces.Tenants;
//using otherServices.Services;

//namespace otherServices.Services.Tenants
//{
//    public class CreditCardService : ICreditCardService
//    {
//        private readonly IUnitOfWork _uow;
//        private readonly ILogger<CreditCardService> _logger;
//        private readonly IMemoryCache _cache;
//        private readonly IEncryptionService _enc;

//        public CreditCardService( IEncryptionService enc, IUnitOfWork uow, IMemoryCache cache, ILogger<CreditCardService> logger)
//        {
//            _enc = enc;
//            _uow = uow;
//            _cache = cache;
//            _logger = logger;
//        }

//        public async Task<CreditCardDto> AddCreditCardAsync(long userId, CreateCreditCardDto dto)
//        {
//            var user = await _uow.Users.GetByIdAsync(userId);
//            if (user == null)
//                throw new KeyNotFoundException("User not found.");

//            // Encrypt card
//            var encryptedNumber = _enc.Encrypt(dto.CardNumber);

//            // Prevent duplicate cards
//            if (await CardExistsForUser(userId, dto.CardNumber))
//                throw new Exception("This credit card already exists for this user.");

//            var detectedType = DetectCardType(dto.CardNumber);

//            var card = new CreditCard
//            {
//                UserId = userId,
//                CardHolderName = dto.CardHolderName,
//                CardNumber = encryptedNumber,
//                ExpiryDate = dto.ExpiryDate,
//                CVV = _enc.Encrypt(dto.CVV),
//                CardType = detectedType
//            };

//            await _uow.CreditCards.AddAsync(card);
//            await _uow.CompleteAsync();

//            return new CreditCardDto
//            {
//                CreditCardId = card.CreditCardId,
//                UserId = card.UserId,
//                MaskedCardNumber = _enc.Mask(dto.CardNumber),
//                CardHolderName = card.CardHolderName,
//                ExpiryDate = card.ExpiryDate,
//                CardType = card.CardType
//            };
//        }

//        // Prevent duplicate credit cards for the same user
//        private async Task<bool> CardExistsForUser(long userId, string plainCardNumber)
//        {
//            var cards = await _uow.CreditCards.FindAsync(c => c.UserId == userId);

//            foreach (var c in cards)
//            {
//                var decrypted = _enc.Decrypt(c.CardNumber);
//                if (decrypted == plainCardNumber)
//                    return true;
//            }

//            return false;
//        }

//        // Auto-detect card type
//        private CardType DetectCardType(string number)
//        {
//            if (string.IsNullOrWhiteSpace(number))
//                return CardType.Unknown;

//            if (number.StartsWith("4"))
//                return CardType.Visa;

//            if (number.StartsWith("5"))
//                return CardType.MasterCard;

//            if (number.StartsWith("34") || number.StartsWith("37"))
//                return CardType.AmericanExpress;

//            return CardType.Unknown;
//        }

//        public async Task<CreditCardDto> EditCreditCardAsync(long userId, long cardId, EditCreditCardDto dto)
//        {
//            // Check user exists
//            var user = await _uow.Users.GetByIdAsync(userId);
//            if (user == null)
//                throw new KeyNotFoundException("User not found.");

//            // Check card exists
//            var card = await _uow.CreditCards.GetByIdAsync(cardId);
//            if (card == null)
//                throw new KeyNotFoundException("Credit card not found.");

//            // Ensure card belongs to this user
//            if (card.UserId != userId)
//                throw new UnauthorizedAccessException("You do not own this credit card.");



//            // 1️⃣ Update card number (optional)
//            if (!string.IsNullOrWhiteSpace(dto.CardNumber))
//            {
//                var encryptedNewNumber = _enc.Encrypt(dto.CardNumber);

//                // Check duplication
//                var otherCards = await _uow.CreditCards.FindAsync(c => c.UserId == userId && c.CreditCardId != cardId);
//                foreach (var c in otherCards)
//                {
//                    var decrypted = _enc.Decrypt(c.CardNumber);
//                    if (decrypted == dto.CardNumber)
//                        throw new Exception("This credit card already exists for this user.");
//                }

//                card.CardNumber = encryptedNewNumber;
//                card.CardType = DetectCardType(dto.CardNumber);
//            }


//            // 2️⃣ Update card holder name (optional)
//            if (!string.IsNullOrWhiteSpace(dto.CardHolderName))
//                card.CardHolderName = dto.CardHolderName;


//            // 3️⃣ Update CVV (optional)
//            if (!string.IsNullOrWhiteSpace(dto.CVV))
//                card.CVV = _enc.Encrypt(dto.CVV);


//            // 4️⃣ Update expiry date (optional)
//            if (dto.ExpiryDate.HasValue)
//                card.ExpiryDate = dto.ExpiryDate.Value;



//            _uow.CreditCards.Update(card);
//            await _uow.CompleteAsync();

//            return new CreditCardDto
//            {
//                CreditCardId = card.CreditCardId,
//                UserId = card.UserId,
//                MaskedCardNumber = _enc.Mask(_enc.Decrypt(card.CardNumber)),
//                CardHolderName = card.CardHolderName,
//                ExpiryDate = card.ExpiryDate,
//                CardType = card.CardType
//            };
//        }

//        public async Task<IEnumerable<CreditCardDto>> GetAllCardsAsync(long userId)
//        {
//            // Check user exists
//            var user = await _uow.Users.GetByIdAsync(userId);
//            if (user == null)
//                throw new KeyNotFoundException("User not found.");

//            // Fetch all cards for this user
//            var cards = await _uow.CreditCards.FindAsync(c => c.UserId == userId);

//            var result = new List<CreditCardDto>();

//            foreach (var card in cards)
//            {
//                var decryptedNumber = _enc.Decrypt(card.CardNumber);

//                result.Add(new CreditCardDto
//                {
//                    CreditCardId = card.CreditCardId,
//                    UserId = card.UserId,
//                    MaskedCardNumber = _enc.Mask(decryptedNumber),
//                    CardHolderName = card.CardHolderName,
//                    ExpiryDate = card.ExpiryDate,
//                    CardType = card.CardType
//                });
//            }

//            return result;
//        }

//        public async Task<CreditCardDto> GetCreditCardByIdAsync(long userId, long cardId)
//        {
//            // Check user exists
//            var user = await _uow.Users.GetByIdAsync(userId);
//            if (user == null)
//                throw new KeyNotFoundException("User not found.");

//            // Fetch card
//            var card = await _uow.CreditCards.GetByIdAsync(cardId);
//            if (card == null)
//                throw new KeyNotFoundException("Credit card not found.");

//            // Ensure this card belongs to the user
//            if (card.UserId != userId)
//                throw new UnauthorizedAccessException("You do not own this credit card.");

//            // Decrypt card number
//            var decryptedNumber = _enc.Decrypt(card.CardNumber);

//            return new CreditCardDto
//            {
//                CreditCardId = card.CreditCardId,
//                MaskedCardNumber = _enc.Mask(decryptedNumber),
//                CardHolderName = card.CardHolderName,
//                ExpiryDate = card.ExpiryDate,
//                CardType = card.CardType
//            };
//        }

//        public async Task<bool> DeleteCreditCardAsync(long userId, long cardId)
//        {
//            var card = await _uow.CreditCards
//                    .FirstOrDefaultAsync(c => c.CreditCardId == cardId && c.UserId == userId);

//            if (card == null)
//                throw new KeyNotFoundException("Credit card not found.");

//            _uow.CreditCards.Remove(card);
//            await _uow.CompleteAsync();

//            return true;
//        }

//    }
//}
