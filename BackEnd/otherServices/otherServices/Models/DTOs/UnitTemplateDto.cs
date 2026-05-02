public class UnitTemplateDto
{
    public string UnitCode { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int NumberOfRooms { get; set; }
    public int NumberOfBathrooms { get; set; }
    public double Area { get; set; }
    public bool IsFurnished { get; set; }
    public bool HasGarage { get; set; }
    public double BasePrice { get; set; }
    public double PriceIncreasePerFloor { get; set; }
    public List<IFormFile>? Images { get; set; }

}
