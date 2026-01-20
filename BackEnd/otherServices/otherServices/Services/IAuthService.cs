using otherServices.Models.DTOs;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace otherServices.Services
{
    public interface IAuthService
    {

        Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO);
        Task<RegisterResponseDTO> Register(RegisterDTO registerDto);
    }
}

