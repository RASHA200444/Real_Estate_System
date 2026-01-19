using otherServices.Models;

namespace otherServices.Services
{
    public interface IJwtService
    {
        string GenerateJwtToken(User user);

        // optional if you want to keep old-style calling
        string GenerateJwtToken(string username, string role, long userId);
    }
}
