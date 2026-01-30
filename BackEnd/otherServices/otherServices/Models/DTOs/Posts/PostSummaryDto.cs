namespace otherServices.Models.DTOs.Posts
{
    public class PostSummaryDto
    {
        public long PostId { get; set; }

        public long UserId { get; set; }
        public string UserName { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }

        public DateTime DatePost { get; set; }

        public List<string> Images { get; set; } = new();

        // ✅ NEW
        public bool IsAuction { get; set; } = false;
    }
}
