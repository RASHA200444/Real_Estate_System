namespace otherServices.Models.DTOs
{
    public class DeleteProjectResultDto
    {
        public long ProjectId { get; set; }
        public int DeletedAvailablePosts { get; set; }
        public int RemainingNonAvailablePosts { get; set; }
        public bool ProjectDeleted { get; set; }
        public string Message { get; set; } = "";
    }
}
