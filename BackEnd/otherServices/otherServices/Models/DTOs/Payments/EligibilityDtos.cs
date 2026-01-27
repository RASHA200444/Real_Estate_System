using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Payments
{
    public class EligibilityAnswerDto
    {
        [Required]
        public string QuestionKey { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;
    }

    public class EligibilityFormRequestDto
    {
        // ✅ minimal structured fields for testing + rule-based decision الآن
        [Range(0, double.MaxValue)]
        public decimal MonthlyIncome { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MonthlyExpenses { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ExistingMonthlyDebt { get; set; }

        public bool HasStableJob { get; set; }

        [Range(0, 50)]
        public int Dependents { get; set; }

        // ✅ optional: allow FE to send any extra Q/A now or later (AI-friendly)
        public List<EligibilityAnswerDto>? ExtraAnswers { get; set; }
    }

    public class EligibilityResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public int? Score { get; set; }
        public string? Reason { get; set; }

        // decisions (one of them relevant depending on flow)
        public int? InstallmentDecision { get; set; } // AIInstallmentDecision
        public int? RentDecision { get; set; }        // AIRentDecision
    }
}
