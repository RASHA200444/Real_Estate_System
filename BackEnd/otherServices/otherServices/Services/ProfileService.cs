using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.Enums;
using otherServices.Repositories;

namespace otherServices.Services
{
    public class ProfileService : IProfileService
    {
        //private readonly IGenericRepository<User> _userRepository;
        //private readonly IGenericRepository<Landlord> _landlordRepository;
        //IRatingsRepository _ratingsRepository;
        ////private readonly IUnitOfWork _uow;
        //private readonly IMediaService _mediaService;
        //private readonly ILogger<ProfileService> _logger;
        //private readonly IPasswordHasher _hasher;

        //public ProfileService( IMediaService mediaService, IRatingsRepository ratingsRepository, ILogger<ProfileService> logger, IPasswordHasher hasher,IGenericRepository<User> userRepository,
        //    IGenericRepository<Landlord> landlordRepository)
        //{
        //    _ratingsRepository = ratingsRepository;
        //    _userRepository = userRepository;
        //    _landlordRepository = landlordRepository;
        //    //_uow = uow;
        //    _mediaService = mediaService;
        //    _logger = logger;
        //    _hasher = hasher;
        //}
        private readonly IRatingsRepository _ratingsRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<Landlord> _landlordRepository;
        private readonly IMediaService _mediaService;
        private readonly ILogger<ProfileService> _logger;
        private readonly IPasswordHasher _hasher;

        public ProfileService(
            IGenericRepository<User> userRepository,
            IGenericRepository<Landlord> landlordRepository,
            IRatingsRepository ratingsRepository,
            IMediaService mediaService,
            ILogger<ProfileService> logger,
            IPasswordHasher hasher)
        {
            _userRepository = userRepository;
            _landlordRepository = landlordRepository;
            _ratingsRepository = ratingsRepository;
            _mediaService = mediaService;
            _logger = logger;
            _hasher = hasher;
        }


        //  GetMyProfile
        public async Task<MyProfileDto> GetMyProfileAsync(int userId)
        {
            _logger.LogInformation("➡️ Fetching MyProfile for UserId: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            var profile = new MyProfileDto
            {
                FullName = user.UserName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                ProfilePhotoPath = user.ProfilePhotoPath,
                NIDPath = user.NIDPath,
            };

            _logger.LogInformation("✅ MyProfile fetched for UserId {UserId}", userId);
            return profile;
        }

        //  GetUserProfile
        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            _logger.LogInformation("➡️ Fetching public profile for UserId: {UserId}", userId);

            // 1️⃣ جلب المستخدم
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            decimal rate = 0;

            // 2️⃣ لو المستخدم Landlord، نجيب الـ Rate من RatingsRepository
            if (user.RoleName == UserRole.Landlord)
            {
                // أولًا ناخد الـ Landlord المرتبط بالـ User
                var landlord = (await _landlordRepository.FindAsync(l => l.UserId == userId)).FirstOrDefault();
                if (landlord != null)
                {
                    // نجيب متوسط التقييم من RatingsRepository
                    rate = await _ratingsRepository.GetUserAverageRatingAsync(landlord.LandlordId);
                }
            }

            // 3️⃣ تحضير DTO
            var dto = new UserProfileDto
            {
                FullName = user.UserName,
                ProfilePhotoPath = user.ProfilePhotoPath,
                Rate = rate
            };

            _logger.LogInformation("✅ Public profile fetched for UserId {UserId}", userId);
            return dto;
        }

        //  UpdateMyProfile
        public async Task UpdateMyProfileAsync(int userId, UpdateProfileDto dto)
        {
            _logger.LogInformation("➡️ Starting UpdateMyProfileAsync for UserId {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            if (dto.ProfilePhoto != null)
                user.ProfilePhotoPath = await _mediaService.SaveFileAsync(dto.ProfilePhoto);

            if (dto.NIDFile != null)
                user.NIDPath = await _mediaService.SaveFileAsync(dto.NIDFile);

            user.UserName = dto.FullName ?? user.UserName;
            user.Phone = dto.Phone ?? user.Phone;
            user.Address = dto.Address ?? user.Address;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("✅ Profile updated for UserId {UserId}", userId);
        }

        //  UpdatePassword
        public async Task UpdatePasswordAsync(int userId, UpdatePasswordDto dto)
        {
            _logger.LogInformation("➡️ Starting UpdatePasswordAsync for UserId {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // التحقق من كلمة المرور القديمة
            if (!_hasher.Verify(user.Password, dto.OldPassword))
                throw new UnauthorizedAccessException("Old password is incorrect.");

            // منع تغيير الباسورد أكثر من مرة في الأسبوع
            if (user.LastPassChange.HasValue &&
                (DateTime.UtcNow - user.LastPassChange.Value).TotalDays < 7)
            {
                throw new InvalidOperationException("Password can only be changed once per week.");
            }

            // حفظ الباسورد الجديد
            user.Password = _hasher.Hash(dto.NewPassword);
            user.LastPassChange = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("✅ Password updated for UserId {UserId}", userId);
        }


    }
}