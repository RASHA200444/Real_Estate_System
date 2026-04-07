using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace otherServices.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        // 1. Property سهلة للقراءة السريعة
        protected long? CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst("uid")?.Value
                               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return long.TryParse(userIdClaim, out var id) ? id : null;
            }
        }

        protected string? CurrentUserName =>
            User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        protected string? CurrentUserRole =>
            User.FindFirst(ClaimTypes.Role)?.Value;

        protected string? TokenId =>
            User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

        // 2. الميثود المطلوبة (المهمة جداً للأخطاء اللي ظهرت عندك)
        protected IActionResult? RequireUserId(out long userId)
        {
            var id = CurrentUserId;
            if (id == null)
            {
                userId = 0;
                return Unauthorized(new { success = false, message = "User ID missing or invalid in token." });
            }

            userId = id.Value;
            return null;
        }

        // 3. للمطالبة بأي كليم آخر بشكل إلزامي
        protected IActionResult? RequireClaim(string claimType, out string value)
        {
            value = User.FindFirst(claimType)?.Value ?? "";
            if (string.IsNullOrWhiteSpace(value))
            {
                return Unauthorized(new { success = false, message = $"Required claim '{claimType}' is missing." });
            }
            return null;
        }

        protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

        protected IActionResult UnauthorizedMissingInfo(string infoName)
            => Unauthorized(new { success = false, message = $"Invalid token: {infoName} is missing." });
    }
}