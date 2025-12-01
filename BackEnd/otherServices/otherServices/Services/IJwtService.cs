using WebAPIDotNet.DTOs;

namespace otherServices.Services
{
    public interface IJwtService
    {
        string GenerateJwtToken(string username,string role);

    }
}
