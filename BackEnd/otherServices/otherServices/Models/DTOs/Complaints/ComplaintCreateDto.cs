using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Complaints
{
    public class ComplaintCreateDto
    {
        [Required(ErrorMessage = " Reported UserName is Reqired")]
        public string ReportedUserName { get; set; }
        public ComplaintType Type { get; set; }

        [Required]
        public string Content { get; set; }
        public IFormFile? Image { get; set; }
    }

}
