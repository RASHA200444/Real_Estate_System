using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using otherServices.Models;

namespace otherServices.Controllers
{
    [ApiController]
    [Route("api/ai/debug")]
    public class AiDebugController : ControllerBase
    {
        private readonly AppDbContext2 _db;

        public AiDebugController(AppDbContext2 db)
        {
            _db = db;
        }

        // See last outbox rows
        [HttpGet("outbox")]
        public async Task<IActionResult> Outbox([FromQuery] int take = 20)
        {
            take = Math.Clamp(take, 1, 200);

            var rows = await _db.AiOutboxMessages
                .OrderByDescending(x => x.AiOutboxMessageId)
                .Take(take)
                .Select(x => new
                {
                    x.AiOutboxMessageId,
                    x.RequestId,
                    x.RequestType,
                    x.EntityType,
                    x.EntityId,
                    x.Attempts,
                    x.SentAtUtc,
                    x.LastError,
                    x.CreatedAtUtc
                })
                .ToListAsync();

            return Ok(rows);
        }

        // Get module results for entity
        [HttpGet("results/{entityType}/{entityId:long}")]
        public async Task<IActionResult> Results(string entityType, long entityId, [FromQuery] int take = 20)
        {
            take = Math.Clamp(take, 1, 200);

            var rows = await _db.Set<AiModuleResult>()
                .AsNoTracking()
                .Where(r => r.EntityType == entityType && r.EntityId == entityId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Take(take)
                .Select(r => new
                {
                    r.RequestId,
                    r.RequestType,
                    r.Score,
                    r.Reason,
                    r.CreatedAtUtc,
                    r.PayloadJson
                })
                .ToListAsync();

            return Ok(rows);
        }

        // Get result by requestId
        [HttpGet("result/by-request/{requestId}")]
        public async Task<IActionResult> ResultByRequest(string requestId)
        {
            var row = await _db.Set<AiModuleResult>()
                .AsNoTracking()
                .Where(r => r.RequestId == requestId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (row == null) return NotFound();

            return Ok(new
            {
                row.RequestId,
                row.RequestType,
                row.EntityType,
                row.EntityId,
                row.Score,
                row.Reason,
                row.CreatedAtUtc,
                row.PayloadJson
            });
        }
    }
}
