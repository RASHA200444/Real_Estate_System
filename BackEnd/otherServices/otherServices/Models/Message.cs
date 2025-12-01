namespace otherServices.Models
{
    public partial class Message
    {
        public long MessageId { get; set; }

        public long SenderId { get; set; } // بدل LandlordId

        public long ReceiverId { get; set; } // بدل TenantId

        public string Message1 { get; set; } = null!;

        public DateTime DateMessge { get; set; }

        public virtual User Sender { get; set; } = null!; // بدل Landlord

        public virtual User Receiver { get; set; } = null!; // بدل Tenant
    }
}
