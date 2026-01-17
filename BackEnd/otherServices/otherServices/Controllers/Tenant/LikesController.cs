using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models;
using otherServices.Services.Interfaces.Tenants;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace otherServices.Controllers.Tenant
{
    [Route("api/[controller]")]
    [ApiController]
    public class LikesController : ControllerBase
    {
        private readonly ILikeService service;

        public LikesController(ILikeService service)
        {
            this.service = service;
        }

        [HttpGet("Count/{PostId}")]
        public async Task<IActionResult> GetLikesCountAsync(int PostId)
        {
            try
            {
                var result = await service.GetLikeCountByPostIdAsync(PostId);
                return Ok(new
                {
                    count = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("Like/{UserId}/{PostId}")]
        public async Task<IActionResult> LikePostAsync( int UserId, int PostId)
        {
            try
            {
                await service.LikePostAsync(UserId, PostId);
                return Ok(new {Status = "Post has Liked successfully"});
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("CancelLike/{UserId}/{PostId}")]
        public async Task<IActionResult> GetUserProfile(int UserId, int PostId)
        {
            try
            {
                await service.RemoveLikeAsync(UserId, PostId);
                return Ok(new { Status = "Like has removed successfully"});
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}
