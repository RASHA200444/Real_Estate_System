using Microsoft.Extensions.Caching.Memory;
using otherServices.Models.Enums;
using otherServices.Models;
using System.Diagnostics;
using otherServices.Repositories;
using otherServices.Models.DTOs.Complaints;
using otherServices.Services.Interfaces.Tenants;
using Microsoft.EntityFrameworkCore;

namespace otherServices.Services.Tenants
{
    public class ComplaintService : IComplaintService
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMediaService mediaService;
        private readonly IMemoryCache cache;
        private readonly ILogger<ComplaintService> logger;

        private const string ComplaintsCacheKey = "complaints_pending";

        public ComplaintService(
            IUnitOfWork unitOfWork,
            IMediaService mediaService,
            IMemoryCache cache,
            ILogger<ComplaintService> logger)
        {
            this.unitOfWork = unitOfWork;
            this.mediaService = mediaService;
            this.cache = cache;
            this.logger = logger;
        }

        public async Task CreateComplaintAsync(long ReporterUserId ,ComplaintCreateDto dto)
        {
            logger.LogInformation("Creating new complaint...");

            var reporter = await unitOfWork.Users.GetByIdAsync(ReporterUserId)
                ?? throw new ArgumentException("Invalid reporter user ID.");

            var reportedUser = (await unitOfWork.Users.FindAsync(u => u.UserName == dto.ReportedUserName))
                .FirstOrDefault()
                ?? throw new ArgumentException("Reported user not found.");

            string? filePath = null;
            if (dto.Image != null)
                filePath = await mediaService.SaveFileAsync(dto.Image);

            var complaint = new Complaint
            {
                ReporterUserId = ReporterUserId,
                ReportedUserId = reportedUser.UserId,
                Type = dto.Type,
                Content = dto.Content,
                ImagePath = filePath,
                Status = ComplaintStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await unitOfWork.Complaints.AddAsync(complaint);
            await unitOfWork.CompleteAsync();

            // 🧹 Clear cache
            cache.Remove(ComplaintsCacheKey);

            logger.LogInformation("Complaint created and cache cleared.");
        }

        public async Task<IEnumerable<ComplaintDto>> GetComplaintsAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            logger.LogInformation(" Fetching complaints...");

            if (cache.TryGetValue(ComplaintsCacheKey, out IEnumerable<ComplaintDto>? cachedComplaints))
            {
                logger.LogInformation(" Complaints retrieved from cache.");
                stopwatch.Stop();
                logger.LogInformation($" Execution time: {stopwatch.ElapsedMilliseconds} ms");
                return cachedComplaints!;
            }

            var complaints = await unitOfWork.Complaints
                .GetAllQueryable()
                .Include(c => c.ReporterUser)
                .Include(c => c.ReportedUser)
                .Where(c => c.Status == ComplaintStatus.Pending)
                .Select(c => new ComplaintDto
                {
                    ComplaintId = c.ComplaintId,
                    ReporterUserId = c.ReporterUserId,
                    ReportedUserId = c.ReportedUserId,
                    ReporterName = c.ReporterUser.UserName,
                    ReportedName = c.ReportedUser.UserName,
                    Type = c.Type,
                    Content = c.Content,
                    ImagePath = c.ImagePath, 
                    Status = c.Status,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            if (complaints.Count == 0)
                throw new ArgumentException("No complaints found.");

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(3));

            cache.Set(ComplaintsCacheKey, complaints, cacheOptions);

            stopwatch.Stop();
            logger.LogInformation(" Complaints cached.");
            logger.LogInformation($" DB fetch time: {stopwatch.ElapsedMilliseconds} ms");

            return complaints;
        }

        public async Task<ComplaintDetailsDto> GetComplaintByIdAsync(int complaintId)
        {
            logger.LogInformation($" Fetching complaint by ID: {complaintId}");

            var complaint = await unitOfWork.Complaints
                .GetAllQueryable()
                .Include(c => c.ReporterUser)
                .Include(c => c.ReportedUser)
                .FirstOrDefaultAsync(c => c.ComplaintId == complaintId)
                ?? throw new KeyNotFoundException($"Complaint with ID {complaintId} not found.");

            return new ComplaintDetailsDto
            {
                ComplaintId = complaint.ComplaintId,
                ReporterUserId = complaint.ReporterUserId,
                ReportedUserId = complaint.ReportedUserId,
                ReporterName = complaint.ReporterUser?.UserName ?? "Unknown",
                ReportedName = complaint.ReportedUser?.UserName ?? "Unknown",
                ReporterPhone = complaint.ReporterUser?.Phone ?? "N/A",
                ReporterEmail = complaint.ReporterUser?.Email ?? "N/A",
                Type = complaint.Type,
                Content = complaint.Content,
                ImagePath = complaint.ImagePath, 
                Status = complaint.Status,
                CreatedAt = complaint.CreatedAt
            };
        }

        // Ban user
        public async Task BanUserAsync(int complaintId)
        {
            var complaint = await unitOfWork.Complaints.GetByIdAsync(complaintId)
                ?? throw new ArgumentException("Complaint not found.");

            var user = await unitOfWork.Users
                                            .GetAllQueryable()
                                            .FirstOrDefaultAsync(o => o.UserId == complaint.ReportedUserId)
                                            ?? throw new ArgumentException("Reported user not found.");

            user.ComPanStatus = ComPanStatus.Banned;
            complaint.Status = ComplaintStatus.ActionTaken;

            unitOfWork.Users.Update(user);
            unitOfWork.Complaints.Update(complaint);
            await unitOfWork.CompleteAsync();

            cache.Remove(ComplaintsCacheKey);
            logger.LogInformation($" Owner user with UserId {user.UserId} banned. Cache cleared.");
        }

        // Suspend user
        public async Task SuspendUserAsync(int complaintId, int days)
        {
            var complaint = await unitOfWork.Complaints.GetByIdAsync(complaintId)
                ?? throw new ArgumentException("Complaint not found.");

            var user = await unitOfWork.Users
                                        .GetAllQueryable().
                                        FirstOrDefaultAsync(O => O.UserId == complaint.ReportedUserId)
                ?? throw new ArgumentException("Reported user not found.");

            user.ComPanStatus = ComPanStatus.Suspend;
            user.SuspendedUntil = DateTime.UtcNow.AddDays(days);
            complaint.Status = ComplaintStatus.ActionTaken;

            unitOfWork.Users.Update(user);
            unitOfWork.Complaints.Update(complaint);
            await unitOfWork.CompleteAsync();

            cache.Remove(ComplaintsCacheKey);
            logger.LogInformation($" User With UserId {user.UserId} suspended for {days} days. Cache cleared.");
        }

        // Refuse complaint
        public async Task RefuseComplaintAsync(int complaintId)
        {
            var complaint = await unitOfWork.Complaints.GetByIdAsync(complaintId)
                ?? throw new ArgumentException("Complaint not found.");

            complaint.Status = ComplaintStatus.Rejected;

            unitOfWork.Complaints.Update(complaint);
            await unitOfWork.CompleteAsync();

            cache.Remove(ComplaintsCacheKey);
            logger.LogInformation($"❌ Complaint {complaintId} refused. Cache cleared.");
        }
    }

}
