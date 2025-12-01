//using Microsoft.AspNetCore.Mvc;
//using System.ComponentModel.DataAnnotations;

//namespace WebAPIDotNet.DTOs
//{
//    public class UpdatePostDTO
//    {

//        [FromForm]
//        [StringLength(255)]
//        public string Title { get; set; }
//        [FromForm]
//        public string Description { get; set; }
//        [FromForm]
//        public double? Price { get; set; }
//        [FromForm]
//        [StringLength(255)]
//        public string Location { get; set; }
//        [FromForm]
//        [StringLength(50)]
//        public string RentalStatus { get; set; }

//        [FromForm]
//        public IFormFile File { get; set; }
//    }
//}


using otherServices.Models.Enums;

public class UpdatePostDTO
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public double? Price { get; set; }
    public string? Location { get; set; }
    public string? LocationPath { get; set; }
    public PropertyStatus? RentalStatus { get; set; }

    //public IFormFile? PostDocFile { get; set; }  // للتحديث، مش شرط إجباري

    //public List<IFormFile>? NewImages { get; set; } // صور جديدة للإضافة
    //public List<string>? ImagesToDelete { get; set; }  // صور يتم حذفها
}
