namespace otherServices.Models.DTOs
{
    public class PostsCommentsDto
    {
        public long CommentId { get; set; }
        public long PostId { get; set; }
        public string Comment_Written { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
