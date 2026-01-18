namespace otherServices.Models.DTOs.Payments
{
    public class TransferWithFeeResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public decimal? PayerRemainingBalance { get; set; }
        public decimal? PayeeBalance { get; set; }
        public decimal? AdminBalance { get; set; }

        public decimal FeeAmount { get; set; }
        public decimal NetToPayee { get; set; }
    }
}
