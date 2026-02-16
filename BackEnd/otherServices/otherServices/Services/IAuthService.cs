using otherServices.Models.DTOs;
using System;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace otherServices.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO);
        Task<RegisterResponseDTO> Register(RegisterDTO registerDto);

        // ✅ used by /2fa/verify
        Task<(string accessToken, string refreshToken, DateTime refreshExp)> CompleteTwoFactorLoginAsync(long userId);
    }
}
