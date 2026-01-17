namespace otherServices.Services
{
    public interface IMediaService
    {
        Task<string?> SaveFileAsync(IFormFile file);
        bool DeleteFile(string? relativePath);

    }
}
