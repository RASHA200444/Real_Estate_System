using otherServices.Models.DTOs;

namespace WebAPIDotNet.DTOs
{
    public class LoginResponseDTO
    {
        // Access Token (JWT)
        public string? Token { get; set; }

        // ✅ NEW: refresh token (يفضل يتبعت Cookie مش Body)
        public string? RefreshToken { get; set; }

        // ✅ NEW: expiry بتاع refresh
        public DateTime? RefreshTokenExpiresAt { get; set; }

        public UserDataDTO User { get; set; }




        public bool? TwoFactorRequired { get; set; }
        public string? TwoFactorToken { get; set; }

    }
}
