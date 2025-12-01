using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace otherServices.Models
{
    public class PostImage
    {
        public long ImageId { get; set; }
        public long PostId { get; set; }
        public string ImageUrl { get; set; }

        public Post Post { get; set; }

    }
}
