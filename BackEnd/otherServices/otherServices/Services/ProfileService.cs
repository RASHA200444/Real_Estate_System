using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.Enums;
using otherServices.Repositories;

namespace otherServices.Services
{
    public class ProfileService : IProfileService
    {
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
            _logger.LogInformation("Fetching MyProfile for UserId: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            bool isPro = false;
            string? ownershipDocPath = null;

            if (user.RoleName == UserRole.Landlord)
            {
                var landlord = (await _landlordRepository.FindAsync(l => l.UserId == userId)).FirstOrDefault();
                if (landlord != null)
                {
                    isPro = landlord.IsPro;
                    ownershipDocPath = landlord.OwnershipDocPath;
                }
            }

            var profile = new MyProfileDto
            {
                FullName = user.UserName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                ProfilePhotoPath = user.ProfilePhotoPath,
                NIDPath = user.NIDPath,

                // ✅ fill from landlord if exists
                OwnershipDocumentPath = ownershipDocPath,

                // ✅ NEW
                IsPro = isPro
            };

            _logger.LogInformation("MyProfile fetched for UserId {UserId}", userId);
            return profile;
        }


        //  GetUserProfile
        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            _logger.LogInformation("Fetching public profile for UserId: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            decimal rate = 0;
            bool isPro = false;

            if (user.RoleName == UserRole.Landlord)
            {
                var landlord = (await _landlordRepository.FindAsync(l => l.UserId == userId)).FirstOrDefault();
                if (landlord != null)
                {
                    rate = await _ratingsRepository.GetUserAverageRatingAsync(landlord.LandlordId);
                    isPro = landlord.IsPro; // ✅ NEW
                }
            }

            var dto = new UserProfileDto
            {
                FullName = user.UserName,
                ProfilePhotoPath = user.ProfilePhotoPath,
                Rate = rate,

                // ✅ NEW
                IsPro = isPro
            };

            _logger.LogInformation("Public profile fetched for UserId {UserId}", userId);
            return dto;
        }


        //  UpdateMyProfile
        public async Task UpdateMyProfileAsync(int userId, UpdateProfileDto dto)
        {
            _logger.LogInformation("Starting UpdateMyProfileAsync for UserId {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            if (dto.Username != null)
            {
                var usernameExists = (await _userRepository.FindAsync(u => u.UserName == dto.Username)).Any();
                if (usernameExists)
                    throw new Exception("Username already exists");
            }

            if (dto.Email != null)
            {
                var emailExists = (await _userRepository.FindAsync(u => u.Email == dto.Email)).Any();
                if (emailExists)
                    throw new Exception("Email already exists");
            }

            if (dto.ProfilePhoto != null)
                user.ProfilePhotoPath = await _mediaService.SaveFileAsync(dto.ProfilePhoto);

            if (dto.NIDFile != null)
                user.NIDPath = await _mediaService.SaveFileAsync(dto.NIDFile);

            user.UserName = dto.Username ?? user.UserName;
            user.Phone = dto.Phone ?? user.Phone;
            user.Email = dto.Email ?? user.Email;
            user.Address = dto.Address ?? user.Address;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Profile updated for UserId {UserId}", userId);
        }

        //  UpdatePassword
        public async Task UpdatePasswordAsync(int userId, UpdatePasswordDto dto)
        {
            _logger.LogInformation("Starting UpdatePasswordAsync for UserId {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (!_hasher.Verify(user.Password, dto.OldPassword))
                throw new UnauthorizedAccessException("Old password is incorrect.");

            if (user.LastPassChange.HasValue &&
                (DateTime.UtcNow - user.LastPassChange.Value).TotalDays < 7)
            {
                throw new InvalidOperationException("Password can only be changed once per week.");
            }

            user.Password = _hasher.Hash(dto.NewPassword);
            user.LastPassChange = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Password updated for UserId {UserId}", userId);
        }


        public async Task DeleteProfileAsync(int userId, string password)
        {
            _logger.LogInformation("Starting DeleteProfileAsync for UserId {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (!_hasher.Verify(user.Password, password))
                throw new UnauthorizedAccessException("Password is incorrect.");

            _userRepository.Remove(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted successfully.", userId);
        }

    }
}