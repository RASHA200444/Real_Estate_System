using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class PostDTo
{
    public long PostId { get; set; }

    // Landlord
    public long UserId { get; set; }
    public long LandlordId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }

    // Post
    public string Title { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public PriceEvaluation PriceEvaluation { get; set; } // VeryLow = -2, Low = -1, Acceptable = 0, High = 1, VeryHigh = 2

    public string Location { get; set; }
    public string LocationPath { get; set; }
    public string PostDocPath { get; set; }
    public AIDecision PostDocPathEvaluation { get; set; }  // NotReviewed = 0, Verified = 1, Fraudulent = 2, Uncertain = 3  


    // Apartment specifications
    public int NumOfRooms { get; set; }
    public int NumOfBathrooms { get; set; }
    public double Area { get; set; } // in square meters
    public int? TotalUnitsInBuilding { get; set; } // nullable

    // Additional optional attributes
    public bool IsFurnished { get; set; }
    public bool HasGarage { get; set; }
    public int? FloorNumber { get; set; }

    public DateTime? StartRentalDate { get; set; }
    public DateTime? EndRentalDate { get; set; }
    public DateTime DatePost { get; set; }

    public PostPendingStatus FlagWaitingPost { get; set; } // refused , Pending , Accepted
    public PropertyStatus RentalStatus { get; set; } // Available / Sold / UnderNegotiation
    public PropertyType RentType { get; set; } // Rent / Sale


    public List<string> Images { get; set; }

    public List<string>? Tags { get; set; }   // user enters tags in UI -> sent as array


}





//using otherServices.Models.Enums;

//namespace otherServices.Models.DTOs
//{
//    //public class PostDTo
//    //{
//    //    public long PostId { get; set; }
//    //    public string Title { get; set; }
//    //    public string Description { get; set; }
//    //    public double Price { get; set; }
//    //    public string Location { get; set; }
//    //    public PropertyStatus RentalStatus { get; set; }
//    //    public DateTime CreatedAt { get; set; }
//    //    public PostPendingStatus FlagWaitingPost { get; set; }
//    //    public long UserId { get; set; }
//    //    public string UserName { get; set; }
//    //    public string Email { get; set; }

//    //    public string ImagePath { get; set; }
//    //    public string FileBase64 { get; set; }



//    //}

//    public class PostDTo
//    {
//        public long PostId { get; set; }
//        public string Title { get; set; }
//        public string Description { get; set; }
//        public double Price { get; set; }
//        public string Location { get; set; }
//        public PropertyStatus RentalStatus { get; set; }
//        public DateTime CreatedAt { get; set; }
//        public PostPendingStatus FlagWaitingPost { get; set; }
//        public long UserId { get; set; }
//        public string UserName { get; set; }
//        public string Email { get; set; }

//        public List<string> ImagePaths { get; set; }   // ← List of Image URLs
//        public List<string> FilesBase64 { get; set; }  // ← List of Image Base64
//    }




//}

