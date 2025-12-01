using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace otherServices.Models
{
    public class Like
    {
        public long UserId { get; set; }
        public long PostId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; }
        public Post Post { get; set; }
    }

}
