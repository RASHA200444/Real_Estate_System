using Microsoft.EntityFrameworkCore.Metadata.Internal;
using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class Transaction
    {
        public long TransactionId { get; set; }

        public long PostId { get; set; }
        public Post Post { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }


        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public TransactionStatus Status { get; set; } // purchased / installment / under_negotiation
        public DateTime CreatedAt { get; set; }

    }
}
