using otherServices.Models;
using System.Security.Claims;

namespace otherServices.Services
{
    public interface IJwtService
    {
        string GenerateJwtToken(User user);

        // optional if you want to keep old-style calling
        string GenerateJwtToken(string username, string role, long userId);

        // ✅ NEW: refresh token helpers
        string GenerateRefreshToken();
        string HashRefreshToken(string refreshToken);



        string GenerateTwoFactorToken(User user, int expiresMinutes = 5);
        ClaimsPrincipal? ValidateTwoFactorToken(string token);

}


}
