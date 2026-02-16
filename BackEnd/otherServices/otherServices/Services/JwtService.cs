using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using otherServices.Models;

namespace otherServices.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ✅ Access Token (JWT)
        public string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.RoleName.ToString()),
                new Claim("uid", user.UserId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateJwtToken(string username, string role, long userId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("uid", userId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ✅ NEW: Generate refresh token (raw value)
        public string GenerateRefreshToken()
        {
            // 64 bytes random -> base64url string
            var bytes = RandomNumberGenerator.GetBytes(64);
            var b64 = Convert.ToBase64String(bytes);

            // base64url without padding
            return b64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        // ✅ NEW: Hash refresh token before saving in DB
        public string HashRefreshToken(string refreshToken)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(refreshToken);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash); // 64 hex chars
        }




        public string GenerateTwoFactorToken(User user, int expiresMinutes = 5)
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
            {
                new Claim("tfa", "1"),
                new Claim("uid", user.UserId.ToString()),
                new Claim("un", user.UserName),
            };

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            public ClaimsPrincipal? ValidateTwoFactorToken(string token)
            {
                var handler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

                try
                {
                    var principal = handler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = _configuration["Jwt:Issuer"],
                        ValidAudience = _configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.FromSeconds(30)
                    }, out _);

                    if (principal.FindFirst("tfa")?.Value != "1") return null;
                    return principal;
                }
                catch
                {
                    return null;
                }
            }

    }
}
