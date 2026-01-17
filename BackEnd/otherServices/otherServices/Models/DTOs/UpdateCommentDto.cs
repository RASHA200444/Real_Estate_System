using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs
{
    public class UpdateCommentDto
    {
        [Required]
        public string Comment_description { get; set; }
    }
}