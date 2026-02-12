namespace otherServices.Models.DTOs.Payments
{
    public class TenantPaymentPlanDto
    {
        public long PlanId { get; set; }
        public long PostId { get; set; }

        public string PropertyType { get; set; } = "";
        public string PlanType { get; set; } = ""; // Cash / Installment

        public decimal PeriodicAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = "";

        public bool CanPayRemaining { get; set; }
    }

    public class TenantPaymentPlanDetailsDto
    {
        public long PlanId { get; set; }
        public long PostId { get; set; }

        public string PropertyType { get; set; } = "";
        public string PlanType { get; set; } = "";

        public decimal PeriodicAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? DurationMonths { get; set; }
        public int? IntervalMonths { get; set; }

        public List<TenantScheduleDto> Schedules { get; set; } = new();
    }

    public class TenantScheduleDto
    {
        public long ScheduleId { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }

        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }

        public bool CanPayNow { get; set; }
    }
}
