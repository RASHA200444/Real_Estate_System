using System;

namespace CommentAPI.DTOs
{
    public class CommentDto
    {
        public long CommentId { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; }
        public long UserId { get; set; }
        public long PostId { get; set; }
    }
}