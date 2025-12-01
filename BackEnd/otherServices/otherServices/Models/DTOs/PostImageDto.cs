namespace otherServices.Models.DTOs
{
    public class PostImageDto
    {
        public long ImageId { get; set; }
        public string ImageUrl { get; set; }
        public string? FileBase64 { get; set; }
    }

}
