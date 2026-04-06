// ===============================
// File: otherServices/Controllers/OwnerForecastsController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using otherServices.Infrastructure.Kafka;
using otherServices.Models;

namespace otherServices.Controllers
{
    [ApiController]
    [Route("api/owner/forecasts")]
    [Authorize] // حماية الكنترولر بالكامل
    public class OwnerForecastsController : BaseApiController // الوراثة من الكلاس الموحد
    {
        private readonly AppDbContext2 _db;
        private readonly IAiRequestDispatcher _ai;

        public OwnerForecastsController(AppDbContext2 db, IAiRequestDispatcher ai)
        {
            _db = db;
            _ai = ai;
        }

        // Enqueue 3 forecast requests for a post
        [HttpPost("{postId:long}/enqueue")]
        public async Task<IActionResult> Enqueue(long postId)
        {
            // استخراج الـ UserId من التوكن
            if (RequireUserId(out var userId) is IActionResult error) return error;

            var post = await _db.Posts
                .Include(p => p.PostImages)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null) return NotFound(new { message = "Post not found" });

            // في ملف OwnerForecastsController.cs - السطر 40
            if (post.LandlordId != userId)
                return Forbid("You are not authorized to request forecasts for this property.");

            var payload = new
            {
                postId = post.PostId,
                title = post.Title,
                description = post.Description,
                location = post.Location,
                type = post.Type.ToString(),
                status = post.Status.ToString(),
                isAuction = post.IsAuction,
                price = post.Price,
                area = post.Area,
                numberOfRooms = post.NumberOfRooms,
                numberOfBathrooms = post.NumberOfBathrooms,
                floorNumber = post.FloorNumber,
                isFurnished = post.IsFurnished,
                hasGarage = post.HasGarage,
                tagsJson = post.TagsJson
            };

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var r1 = await _ai.EnqueueAsync(AiRequestTypes.Owner_ForecastPrice, "post", postId, payload);
                var r2 = await _ai.EnqueueAsync(AiRequestTypes.Owner_ForecastDemand, "post", postId, payload);
                var r3 = await _ai.EnqueueAsync(AiRequestTypes.Owner_ForecastRevenue, "post", postId, payload);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { priceRequestId = r1, demandRequestId = r2, revenueRequestId = r3 });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Failed to enqueue forecasts", details = ex.Message });
            }
        }
    }
}