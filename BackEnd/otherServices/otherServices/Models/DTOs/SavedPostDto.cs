using CommentAPI.DTOs;
using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class SavedPostDto
    {
        public long PostId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string Location { get; set; }
        public PropertyStatus RentalStatus { get; set; }
        public PostPendingStatus FlagWaitingPost { get; set; }
        //public string ImagePath { get; set; }
        public string? PostDocPath { get; set; }
        public List<string> Images { get; set; } // 👈 كل روابط الصور
        public DateTime CreatedAt { get; set; }

        // Landlord
        public long landlordId { get; set; }
        public string landlordUserName { get; set; }

        // Comments
        //public List<PostsCommentsDto> Comments { get; set; }
    }
}
