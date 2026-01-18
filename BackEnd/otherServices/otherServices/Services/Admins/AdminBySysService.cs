using Microsoft.Extensions.Caching.Memory;
using otherServices.Models.Enums;
using otherServices.Models;
using System.Diagnostics;
using otherServices.Services.Interfaces;
using otherServices.Repositories;
using otherServices.Models.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace otherServices.Services.Admins
{
    public class AdminBySysService : IAdminBySysService
    {
        private readonly IUnitOfWork _uow;
        private readonly IPasswordHasher _hasher;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AdminBySysService> _logger;

        public AdminBySysService(IUnitOfWork uow, IPasswordHasher hasher, IMemoryCache cache, ILogger<AdminBySysService> logger)
        {
            _uow = uow;
            _hasher = hasher;
            _cache = cache;
            _logger = logger;
        }

        public async Task<AdminDto> CreateAdminAsync(CreateAdminDto dto)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Starting CreateAdminAsync for Email: {Email}", dto.Email);

            var userNameExists = (await _uow.Users.FindAsync(u => u.UserName == dto.UserName)).Any();
            if (userNameExists)
                throw new Exception("UserName already exists");

            var emailExists = (await _uow.Users.FindAsync(u => u.Email == dto.Email)).Any();
            if (emailExists)
                throw new Exception("Email already exists");

            var admin = new Admin
            {
                User = new User
                {
                    UserName = dto.UserName,   
                    Email = dto.Email,
                    Password = _hasher.Hash(dto.Password),
                    RoleName = UserRole.Admin
                },
                Type = AdminType.AdminByAdmin,
                PrivilegeType = dto.PrivilegeType,
            };

            await _uow.Admins.AddAsync(admin);
            await _uow.CompleteAsync();

            _cache.Remove("admins_all");
            sw.Stop();

            _logger.LogInformation("Admin created successfully: {AdminId}. Execution time: {Elapsed} ms", admin.AdminId, sw.ElapsedMilliseconds);

            return new AdminDto
            {
                UserId = admin.User.UserId,
                //AdminId = admin.AdminId,
                UserName = admin.User.UserName,
                Email = admin.User.Email,
                Type = admin.Type,
                PrivilegeType = admin.PrivilegeType
            };
        }

        public async Task<IEnumerable<AdminDto>> GetAllAsync()
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Starting GetAllAsync");

            if (_cache.TryGetValue("admins_all", out IEnumerable<AdminDto>? cachedAdmins))
            {
                sw.Stop();
                _logger.LogInformation("Returned Admins from cache. Execution time: {Elapsed} ms", sw.ElapsedMilliseconds);
                return cachedAdmins!;
            }

            var admins = await _uow.Admins.GetAllAsync(a => a.User);
            var result = admins.Select(a => new AdminDto
            {
                UserId = a.User.UserId,
                //AdminId = a.AdminId,
                UserName = a.User.UserName,
                Email = a.User.Email,
                Type = a.Type,
                PrivilegeType = a.PrivilegeType,
            }).ToList();

            _cache.Set("admins_all", result, TimeSpan.FromMinutes(5));

            sw.Stop();
            _logger.LogInformation("Admins fetched from DB and cached. Execution time: {Elapsed} ms", sw.ElapsedMilliseconds);

            return result;
        }

        public async Task<AdminDto?> GetByUserIdAsync(long userId)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Starting GetByUserIdAsync for UserId: {UserId}", userId);

            string cacheKey = $"admin_user_{userId}";

            if (_cache.TryGetValue(cacheKey, out AdminDto? cachedAdmin))
            {
                sw.Stop();
                _logger.LogInformation(
                    "Fetched Admin for UserId {UserId} from cache. Execution time: {Elapsed} ms",
                    userId, sw.ElapsedMilliseconds
                );
                return cachedAdmin;
            }

            var admin = (await _uow.Admins.NestedFind(
                a => a.UserId == userId,
                a => a.User
            )).FirstOrDefault();

            if (admin == null)
            {
                sw.Stop();
                _logger.LogWarning(
                    "Admin not found for UserId: {UserId}. Execution time: {Elapsed} ms",
                    userId, sw.ElapsedMilliseconds
                );
                return null;
            }

            var dto = new AdminDto
            {
                UserId = admin.User.UserId,
                //AdminId = admin.AdminId,
                UserName = admin.User.UserName,
                Email = admin.User.Email,
                Type = admin.Type,
                PrivilegeType = admin.PrivilegeType
            };

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));

            sw.Stop();
            _logger.LogInformation(
                "Admin for UserId {UserId} fetched and cached. Execution time: {Elapsed} ms",
                userId, sw.ElapsedMilliseconds
            );

            return dto;
        }

        public async Task<(bool Success, string Message)> UpdateAdminAsync(long userId, UpdateAdminDto dto)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Starting UpdateAdminAsync for UserId: {UserId}", userId);

            // ✅ نجيب الـ Admin عن طريق UserId + Include User
            var admin = (await _uow.Admins.NestedFind(
                a => a.UserId == userId,
                a => a.User
            )).FirstOrDefault();

            if (admin == null)
                throw new KeyNotFoundException("Admin not found.");

            // ✅ Username check (غير نفسه)
            if (!string.IsNullOrEmpty(dto.UserName))
            {
                var usernameExists = (await _uow.Users.FindAsync(
                    u => u.UserName == dto.UserName && u.UserId != userId
                )).Any();

                if (usernameExists)
                    throw new Exception("Username already exists");
            }

            // ✅ Email check (غير نفسه)
            if (!string.IsNullOrEmpty(dto.Email))
            {
                var emailExists = (await _uow.Users.FindAsync(
                    u => u.Email == dto.Email && u.UserId != userId
                )).Any();

                if (emailExists)
                    throw new Exception("Email already exists");
            }

            // ✅ Update fields
            if (!string.IsNullOrEmpty(dto.UserName))
                admin.User.UserName = dto.UserName;

            if (!string.IsNullOrEmpty(dto.Email))
                admin.User.Email = dto.Email;

            if (dto.PrivilegeType.HasValue)
                admin.PrivilegeType = dto.PrivilegeType.Value;

            if (!string.IsNullOrEmpty(dto.Password))
                admin.User.Password = _hasher.Hash(dto.Password);

            _uow.Admins.Update(admin);
            await _uow.CompleteAsync();

            // ✅ Cache
            _cache.Remove("admins_all");
            _cache.Remove($"admin_user_{userId}");

            sw.Stop();
            _logger.LogInformation(
                "Admin for UserId {UserId} updated successfully. Execution time: {Elapsed} ms",
                userId, sw.ElapsedMilliseconds
            );

            return (true, "Admin updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteAdminAsync(long userId)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Starting DeleteAdminAsync for UserId: {UserId}", userId);

            var admin = (await _uow.Admins.FindAsync(a => a.UserId == userId))
                .FirstOrDefault();

            if (admin == null)
                throw new KeyNotFoundException("Admin not found.");

            if (admin.Type == AdminType.AdminBySys)
                throw new InvalidOperationException("Cannot delete system admin.");

            _uow.Admins.Remove(admin);
            await _uow.CompleteAsync();

            _cache.Remove("admins_all");
            _cache.Remove($"admin_user_{userId}");

            sw.Stop();
            _logger.LogInformation(
                "Admin for UserId {UserId} deleted successfully. Execution time: {Elapsed} ms",
                userId, sw.ElapsedMilliseconds
            );

            return (true, "Admin deleted successfully.");
        }
    }
}
