using otherServices.Models.DTOs;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace WebAPIDotNet.Services
{
    public interface IAuthService
    {

        Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO);
        Task<RegisterResponseDTO> Register(RegisterDTO registerDto);
    }
}

