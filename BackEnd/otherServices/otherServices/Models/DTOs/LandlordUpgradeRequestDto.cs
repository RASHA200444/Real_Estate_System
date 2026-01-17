using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs
{
    public class LandlordUpgradeRequestDto
    {
        [Required(ErrorMessage = "OwnershipDocument is Required.")]   
        public IFormFile OwnershipDoc { get; set; }
    }
}
