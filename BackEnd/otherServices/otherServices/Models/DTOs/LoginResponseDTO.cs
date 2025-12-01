using otherServices.Models.DTOs;

namespace WebAPIDotNet.DTOs
{
    public class LoginResponseDTO
    {
        public string Token { get; set; }

        public UserDataDTO User { get; set; }
    }
}


