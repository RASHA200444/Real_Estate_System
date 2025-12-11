using FluentValidation;
using otherServices.Models.DTOs.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs
{
    public class UpdateProfileDto
    {
        [Username]
        public string? Username { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [EgyptianPhone]
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public IFormFile? ProfilePhoto { get; set; }
        public IFormFile? NIDFile { get; set; }
    }
}
