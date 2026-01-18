using otherServices.Models.Enums;

public class ProjectResponseDto
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; }
    public string Location { get; set; }
    public ProjectPendingStatus PendingStatus { get; set; }

    // ✅ NEW (اختياري بس مفيد)
    public List<string> Tags { get; set; } = new();
}
