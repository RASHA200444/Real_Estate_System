using System.ComponentModel.DataAnnotations;

namespace WebAPIDotNet.DTOs
{
    public class SavePostRequestDTO
    {
        [Required]
        public long UserId { get; set; }  
    }
}