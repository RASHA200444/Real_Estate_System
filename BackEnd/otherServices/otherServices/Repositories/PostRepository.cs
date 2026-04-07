using Microsoft.EntityFrameworkCore.Metadata.Internal;
using otherServices.Models;
using otherServices.Models.Enums;
using otherServices.Services.Interfaces;

namespace otherServices.Repositories
{
    public class PostRepository :GenericRepository<Post>, IPostRepository
    {
        private readonly AppDbContext2 _context;
        private readonly INotificationService _notificationService;

        public PostRepository(AppDbContext2 context, INotificationService notificationService ) : base(context)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<Post> AcceptPostAsync(long postId)
        {
            var post = await GetByIdAsync(postId, p => p.Landlord);
            if (post == null) throw new KeyNotFoundException("Post not found");

            post.PendingStatus = PostPendingStatus.Accepted;
            post.IsAdminFinalized = true;
            post.AdminFinalizedAtUtc = DateTime.UtcNow;

            await SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                userId: post.Landlord.UserId,
                title: "تم نشر العقار ✅",
                content: $"تمت الموافقة على نشر عقارك ' {post.Title} '، هو الآن متاح للمستأجرين.",
                type: NotificationType.PostApproved,
                targetUrl: $"/properties/{postId}"
            );

            return post;
        }

        public async Task<Post> RejectPostAsync(long postId)
        {
            var post = await GetByIdAsync(postId, p => p.Landlord);
            if (post == null) throw new KeyNotFoundException("Post not found");

            post.PendingStatus = PostPendingStatus.Refused;
            post.IsAdminFinalized = true;
            post.AdminFinalizedAtUtc = DateTime.UtcNow;

            await SaveChangesAsync();
            await _notificationService.SendNotificationAsync(
                userId: post.Landlord.UserId,
                title: "تم رفض الإعلان ❌",
                content: $"تم رفض الإعلان ' {post.Title} ' لعدم استيفاء الشروط.",
                type: NotificationType.PostApproved,
                targetUrl: $"/properties/{postId}"
            );
            return post;
        }

    }
}
