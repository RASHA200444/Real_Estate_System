using System.ComponentModel.DataAnnotations;

namespace CommentAPI.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        public string Comment_description { get; set; }

    }
}