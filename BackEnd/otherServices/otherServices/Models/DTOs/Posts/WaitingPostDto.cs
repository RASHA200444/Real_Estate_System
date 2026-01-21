using otherServices.Models.Enums;

namespace otherServices.Models.DTOs.Posts
{
    public class WaitingPostsDto
    {
        public long PostId { get; set; }

        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }

        public string Location { get; set; }
        public string LocationPath { get; set; }
        public string PostDocPath { get; set; }

        public int NumberOfRooms { get; set; }
        public int NumberOfBathrooms { get; set; }
        public double Area { get; set; }
        public int? TotalUnitsInBuilding { get; set; }

        public bool IsFurnished { get; set; }
        public bool HasGarage { get; set; }
        public int? FloorNumber { get; set; }

        public DateTime? StartRentalDate { get; set; }
        public DateTime? EndRentalDate { get; set; }
        public DateTime DatePost { get; set; }

        public PropertyStatus RentalStatus { get; set; }
        public PropertyType RentType { get; set; }

        public List<string> Images { get; set; } = new();
    }
}
