using System.ComponentModel.DataAnnotations;

namespace CommentAPI.DTOs
{
    public class UpdateCommentDto
    {
        [Required]
        public string Comment_description { get; set; }
    }
}