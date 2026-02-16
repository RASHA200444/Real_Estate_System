using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.TwoFactor;
using otherServices.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace otherServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext2 _context;
        private readonly IJwtService _jwtService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IEncryptionService _encryptionService;

        public AuthController(
            IAuthService authService,
            IConfiguration configuration,
            AppDbContext2 context,
            IJwtService jwtService,
            ITwoFactorService twoFactorService,
            IEncryptionService encryptionService)
        {
            _authService = authService;
            _configuration = configuration;
            _context = context;
            _jwtService = jwtService;
            _twoFactorService = twoFactorService;
            _encryptionService = encryptionService;
        }

        private int GetRefreshExpiryDays()
        {
            var s = _configuration["Jwt:RefreshTokenExpiryDays"];
            if (int.TryParse(s, out var days) && days > 0) return days;
            return 30;
        }

        private CookieOptions BuildRefreshCookieOptions(DateTime expiresAt)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
                Path = "/"
            };
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDTO);

                if (result == null)
                    return Unauthorized(new { message = "Invalid credentials" });

                // لو login العادي رجّع refresh => خزّنه Cookie
                if (!string.IsNullOrWhiteSpace(result.RefreshToken) && result.RefreshTokenExpiresAt.HasValue)
                {
                    Response.Cookies.Append(
                        "refreshToken",
                        result.RefreshToken,
                        BuildRefreshCookieOptions(result.RefreshTokenExpiresAt.Value)
                    );

                    result.RefreshToken = null;
                    result.RefreshTokenExpiresAt = null;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { error = errorMessage });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                var refreshRaw = Request.Cookies["refreshToken"];
                if (string.IsNullOrWhiteSpace(refreshRaw))
                    return Unauthorized(new { message = "Missing refresh token cookie" });

                var hash = _jwtService.HashRefreshToken(refreshRaw);

                var tokenRow = await _context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.TokenHash == hash);

                if (tokenRow == null)
                    return Unauthorized(new { message = "Invalid refresh token" });

                if (tokenRow.RevokedAt != null)
                    return Unauthorized(new { message = "Refresh token revoked" });

                if (tokenRow.ExpiresAt <= DateTime.UtcNow)
                    return Unauthorized(new { message = "Refresh token expired" });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == tokenRow.UserId);
                if (user == null)
                    return Unauthorized(new { message = "User not found" });

                var newAccessToken = _jwtService.GenerateJwtToken(user);

                // rotation
                tokenRow.RevokedAt = DateTime.UtcNow;

                var newRefreshRaw = _jwtService.GenerateRefreshToken();
                var newRefreshHash = _jwtService.HashRefreshToken(newRefreshRaw);
                var newExpiresAt = DateTime.UtcNow.AddDays(GetRefreshExpiryDays());

                await _context.RefreshTokens.AddAsync(new RefreshToken
                {
                    UserId = user.UserId,
                    TokenHash = newRefreshHash,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = newExpiresAt,
                    RevokedAt = null
                });

                await _context.SaveChangesAsync();

                Response.Cookies.Append("refreshToken", newRefreshRaw, BuildRefreshCookieOptions(newExpiresAt));

                return Ok(new { token = newAccessToken });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var refreshRaw = Request.Cookies["refreshToken"];

                if (!string.IsNullOrWhiteSpace(refreshRaw))
                {
                    var hash = _jwtService.HashRefreshToken(refreshRaw);

                    var tokenRow = await _context.RefreshTokens
                        .FirstOrDefaultAsync(t => t.TokenHash == hash);

                    if (tokenRow != null && tokenRow.RevokedAt == null)
                    {
                        tokenRow.RevokedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    Path = "/",
                    Secure = Request.IsHttps,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict
                });

                return Ok(new { success = true, message = "Logged out" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDTO registerDto)
        {
            try
            {
                var response = await _authService.Register(registerDto);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ========================= 2FA =========================

        [Authorize]
        [HttpPost("2fa/setup")]
        public async Task<IActionResult> Setup2FA()
        {
            var uidStr = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(uidStr)) return Unauthorized();

            var userId = long.Parse(uidStr);
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId);
            if (user == null) return Unauthorized();

            if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(user.TwoFactorSecretEncrypted))
            {
                return Ok(new TwoFactorSetupResponseDto { AlreadyEnabled = true });
            }

            var (secretBase32, uri) = _twoFactorService.GenerateSetup(user.Email);

            user.TwoFactorSecretEncrypted = _encryptionService.Encrypt(secretBase32);
            user.TwoFactorEnabled = false;

            await _context.SaveChangesAsync();

            return Ok(new TwoFactorSetupResponseDto
            {
                AlreadyEnabled = false,
                SecretBase32 = secretBase32,
                OtpAuthUri = uri
            });
        }

        [Authorize]
        [HttpPost("2fa/enable")]
        public async Task<IActionResult> Enable2FA([FromBody] TwoFactorEnableDto dto)
        {
            var uidStr = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(uidStr)) return Unauthorized();

            var userId = long.Parse(uidStr);
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(user.TwoFactorSecretEncrypted))
                return BadRequest(new { message = "2FA setup not initialized" });

            var secret = _encryptionService.Decrypt(user.TwoFactorSecretEncrypted);

            if (!_twoFactorService.VerifyCode(secret, dto.Code))
                return BadRequest(new { message = "Invalid code" });

            user.TwoFactorEnabled = true;
            user.TwoFactorLastVerifiedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("2fa/verify")]
        public async Task<IActionResult> Verify2FA([FromBody] TwoFactorVerifyDto dto)
        {
            var principal = _jwtService.ValidateTwoFactorToken(dto.TwoFactorToken);
            if (principal == null) return Unauthorized(new { message = "Invalid twoFactorToken" });

            var uidStr = principal.FindFirst("uid")?.Value;
            if (string.IsNullOrWhiteSpace(uidStr)) return Unauthorized();

            var userId = long.Parse(uidStr);
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId);
            if (user == null) return Unauthorized();

            if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecretEncrypted))
                return BadRequest(new { message = "2FA not enabled" });

            var secret = _encryptionService.Decrypt(user.TwoFactorSecretEncrypted);

            if (!_twoFactorService.VerifyCode(secret, dto.Code))
                return BadRequest(new { message = "Invalid code" });

            // ✅ هنا بقى الحل: ننادي السيرفس (مش CreateAndStoreRefreshTokenAsync)
            var issued = await _authService.CompleteTwoFactorLoginAsync(user.UserId);

            Response.Cookies.Append(
                "refreshToken",
                issued.refreshToken,
                BuildRefreshCookieOptions(issued.refreshExp)
            );

            user.TwoFactorLastVerifiedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { token = issued.accessToken });
        }
    }
}
