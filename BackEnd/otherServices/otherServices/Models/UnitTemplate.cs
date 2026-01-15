namespace otherServices.Models
{
    public class UnitTemplate
    {
        public long UnitTemplateId { get; set; }
        public long ProjectId { get; set; }

        // A/B/C/.. (مكان الشقة في الدور)
        public string UnitCode { get; set; } = null!;

        // مواصفات الشقة (نسخة من Post)
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        public int NumberOfRooms { get; set; }
        public int NumberOfBathrooms { get; set; }
        public double Area { get; set; }

        public bool IsFurnished { get; set; }
        public bool HasGarage { get; set; }

        // تسعير
        public double BasePrice { get; set; }
        public double PriceIncreasePerFloor { get; set; } // فرق بسيط لكل دور

        // Nav
        public Project Project { get; set; } = null!;
    }
}
