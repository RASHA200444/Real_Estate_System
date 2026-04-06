// ===============================
// File: otherServices/Controllers/Tenant/ComplaintController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Complaints;
using otherServices.Services.Interfaces.Tenants;

namespace otherServices.Controllers.Tenant
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // تأمين الكنترولر بالكامل
    public class ComplaintController : BaseApiController // الوراثة من الكلاس الموحد
    {
        private readonly IComplaintService complaintService;

        public ComplaintController(IComplaintService complaintService)
        {
            this.complaintService = complaintService;
        }

        // =========================
        // Tenant Actions (للمستخدمين)
        // =========================

        // تقديم شكوى جديدة
        [HttpPost("create")]
        public async Task<IActionResult> CreateComplaint([FromForm] ComplaintCreateDto dto)
        {
            try
            {
                if (RequireUserId(out var reporterUserId) is IActionResult error) return error;

                await complaintService.CreateComplaintAsync(reporterUserId, dto);
                return Ok(new { success = true, message = "Complaint submitted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // =========================
        // Admin Actions (للأدمن فقط)
        // =========================

        // عرض كل الشكاوى (Pending)
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetComplaints()
        {
            try
            {
                var complaints = await complaintService.GetComplaintsAsync();
                return Ok(complaints);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // عرض شكوى محددة
        [HttpGet("{complaintId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetComplaintById(int complaintId)
        {
            try
            {
                var complaint = await complaintService.GetComplaintByIdAsync(complaintId);
                return Ok(complaint);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // حظر مستخدم (Ban)
        [HttpPut("ban/{complaintId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BanUser(int complaintId)
        {
            try
            {
                await complaintService.BanUserAsync(complaintId);
                return Ok(new { success = true, message = "User banned and complaint resolved." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // إيقاف مؤقت (Suspend)
        [HttpPut("suspend/{complaintId:int}/{days:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SuspendUser(int complaintId, int days)
        {
            try
            {
                await complaintService.SuspendUserAsync(complaintId, days);
                return Ok(new { success = true, message = $"User suspended for {days} days." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // رفض الشكوى
        [HttpPut("refuse/{complaintId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefuseComplaint(int complaintId)
        {
            try
            {
                await complaintService.RefuseComplaintAsync(complaintId);
                return Ok(new { success = true, message = "Complaint refused successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}