using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace otherServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        // ✅ NEW
        private readonly AppDbContext2 _context;
        private readonly IJwtService _jwtService;

        public AuthController(
            IAuthService authService,
            IConfiguration configuration,
            AppDbContext2 context,
            IJwtService jwtService)
        {
            _authService = authService;
            _configuration = configuration;
            _context = context;
            _jwtService = jwtService;
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
                Secure = Request.IsHttps, // ✅ true في HTTPS / production
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
                Path = "/" // أو "/api/auth" لو تحب تضيقها
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

                // ✅ If refresh token موجود، خزّنه Cookie HttpOnly
                if (!string.IsNullOrWhiteSpace(result.RefreshToken) && result.RefreshTokenExpiresAt.HasValue)
                {
                    Response.Cookies.Append(
                        "refreshToken",
                        result.RefreshToken,
                        BuildRefreshCookieOptions(result.RefreshTokenExpiresAt.Value)
                    );

                    // ✅ الأفضل ما نرجعش refresh token في الـ body
                    result.RefreshToken = null;
                    result.RefreshTokenExpiresAt = null;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ NEW: Refresh Access Token + rotate refresh token
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

                // ✅ Generate NEW access token
                var newAccessToken = _jwtService.GenerateJwtToken(user);

                // ✅ ROTATION: revoke old refresh token, create new one
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

                // ✅ update cookie
                Response.Cookies.Append("refreshToken", newRefreshRaw, BuildRefreshCookieOptions(newExpiresAt));

                return Ok(new
                {
                    token = newAccessToken
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ NEW: Logout (revoke refresh token + delete cookie)
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

                // ✅ delete cookie
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
    }
}
