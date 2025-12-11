namespace otherServices.Services
{
    public class MediaService : IMediaService
    {
        private readonly string _uploadsFolder;

        public MediaService(IWebHostEnvironment env)
        {
            // save in media folder
            _uploadsFolder = Path.Combine(env.ContentRootPath, "Media");

            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        public async Task<string?> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            // just allow photo or pdf
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var fileExt = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExt))
                throw new Exception("Invalid file type. Only .jpg, .png, and .pdf are allowed.");

            string uniqueFileName = $"{Guid.NewGuid()}{fileExt}";
            string filePath = Path.Combine(_uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // return file path
            return Path.Combine("Media", uniqueFileName).Replace("\\", "/");
        }

        public bool DeleteFile(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return false;

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }

            return false;
        }
    }

}
