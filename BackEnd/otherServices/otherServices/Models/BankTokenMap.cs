using System.ComponentModel.DataAnnotations;

namespace otherServices.Models
{
    public class BankTokenMap
    {
        public long BankTokenMapId { get; set; }

        public long BankCardId { get; set; }
        public BankCard BankCard { get; set; } = null!;

        [Required, MaxLength(400)]
        public string Token { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
