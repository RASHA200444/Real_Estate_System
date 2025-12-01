namespace otherServices.Models.DTOs
{
    public class WaitingPostDto
    {

        public long PostId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string Location { get; set; }
        public string RentalStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public long FlagWaitingPost { get; set; }
        public long UserId { get; set; } // FK

        public string ImagePath { get; set; }

        public User User { get; set; }
    }
}
