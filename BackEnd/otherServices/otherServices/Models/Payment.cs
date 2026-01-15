using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class Payment
    {
        public long PaymentId { get; set; }

        public long TransactionId { get; set; }
        public Transaction Transaction { get; set; }

        //public long UserId { get; set; }
        //public User User { get; set; }

        public long CreditCardId { get; set; }
        public CreditCard CreditCard { get; set; }

        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; }
        // Success | Failed | Pending

        public string Gateway { get; set; } = "Stripe"; // أو Paymob
        public string? GatewayReference { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}