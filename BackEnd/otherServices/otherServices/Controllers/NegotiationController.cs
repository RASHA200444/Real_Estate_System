// ===============================
// File: otherServices/Controllers/NegotiationController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using otherServices.Infrastructure.Kafka;
using otherServices.Models;
using otherServices.Models.Enums;

namespace otherServices.Controllers
{
    [ApiController]
    [Route("api/negotiation")]
    [Authorize] // تأمين العمليات لضمان وجود مستخدم حقيقي
    public class NegotiationController : BaseApiController // الوراثة من الكلاس الجديد
    {
        private readonly AppDbContext2 _db;
        private readonly IAiRequestDispatcher _ai;

        public NegotiationController(AppDbContext2 db, IAiRequestDispatcher ai)
        {
            _db = db;
            _ai = ai;
        }

        // Tenant asks: what price should I offer? (no DB change)
        [HttpPost("tenant/suggest/{postId:long}")]
        public async Task<IActionResult> TenantSuggest(long postId, [FromBody] TenantNegotiationRequestDto dto)
        {
            // التحقق من هوية المستخدم (اختياري هنا لو مش هتستخدم الـ ID بس مهم للأمان)
            if (RequireUserId(out var _) is IActionResult error) return error;

            var post = await _db.Posts
                .Include(p => p.PostImages)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null) return NotFound(new { message = "Post not found" });

            var payload = new
            {
                postId = post.PostId,
                title = post.Title,
                description = post.Description,
                location = post.Location,
                type = post.Type.ToString(),
                isAuction = post.IsAuction,
                listedPrice = post.Price,
                tenantBudget = dto.Budget,
                tenantMessage = dto.Message,
                images = post.PostImages?.Select(x => x.ImageUrl).ToList()
            };

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var requestId = await _ai.EnqueueAsync(
                    requestType: AiRequestTypes.Negotiation_PriceSuggestion,
                    entityType: "post",
                    entityId: postId,
                    payload: payload
                );

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { requestId });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "AI Dispatch failed", details = ex.Message });
            }
        }

        // Landlord asks: what counter-offer should I send for a proposal?
        [HttpPost("landlord/counter/{proposalId:long}")]
        public async Task<IActionResult> LandlordCounter(long proposalId, [FromBody] LandlordNegotiationRequestDto dto)
        {
            if (RequireUserId(out var _) is IActionResult error) return error;

            var proposal = await _db.Proposals
                .Include(p => p.Post)
                .ThenInclude(x => x.PostImages)
                .FirstOrDefaultAsync(p => p.ProposalId == proposalId);

            if (proposal == null) return NotFound(new { message = "Proposal not found" });
            if (proposal.Post == null) return BadRequest(new { message = "Proposal has no post" });

            var payload = new
            {
                proposalId = proposal.ProposalId,
                postId = proposal.PostId,
                offeredPrice = proposal.Offeredprice,
                postListedPrice = proposal.Post.Price,
                postTitle = proposal.Post.Title,
                postLocation = proposal.Post.Location,
                landlordGoal = dto.Goal,
                landlordMessage = dto.Message,
                isAuction = proposal.Post.IsAuction,
                type = proposal.Post.Type.ToString(),
                images = proposal.Post.PostImages?.Select(x => x.ImageUrl).ToList()
            };

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var requestId = await _ai.EnqueueAsync(
                    requestType: AiRequestTypes.Negotiation_CounterOfferSuggestion,
                    entityType: "proposal",
                    entityId: proposalId,
                    payload: payload
                );

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { requestId });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "AI Dispatch failed", details = ex.Message });
            }
        }
    }

    public sealed class TenantNegotiationRequestDto
    {
        public decimal? Budget { get; set; }
        public string? Message { get; set; }
    }

    public sealed class LandlordNegotiationRequestDto
    {
        public string? Goal { get; set; } // e.g. "Close fast" / "Max profit"
        public string? Message { get; set; }
    }
}