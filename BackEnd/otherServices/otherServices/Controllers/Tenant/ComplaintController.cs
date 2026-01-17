using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Complaints;
using otherServices.Services.Interfaces.Tenants;

namespace otherServices.Controllers.Tenant
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintController : ControllerBase
    {
        private readonly IComplaintService complaintService;

        public ComplaintController(IComplaintService complaintService)
        {
            this.complaintService = complaintService;
        }

        // عرض كل الشكاوى (Pending)
        [HttpGet("pending")]
        public async Task<IActionResult> GetComplaints()
        {
            try { 
            var complaints = await complaintService.GetComplaintsAsync();
            return Ok(complaints);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // عرض شكوى محددة
        [HttpGet("{complaintId}")]
        public async Task<IActionResult> GetComplaintById(int complaintId)
        {
            try { 
            var complaint = await complaintService.GetComplaintByIdAsync(complaintId);
            return Ok(complaint);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // شكوى جديدة
        [HttpPost("{ReporterUserId:long}/create")]
        public async Task<IActionResult> CreateComplaint(long ReporterUserId, [FromForm] ComplaintCreateDto dto)
        {
            try { 
            await complaintService.CreateComplaintAsync(ReporterUserId , dto);
            return Ok(new { success = true, message = "Complaint submitted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Ban user
        [HttpPut("ban/{complaintId}")]
        public async Task<IActionResult> BanUser(int complaintId)
        {
            try { 
            await complaintService.BanUserAsync(complaintId);
            return Ok(new { success = true, message = "User banned and complaint resolved." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Suspend user
        [HttpPut("suspend/{complaintId}/{days}")]
        public async Task<IActionResult> SuspendUser(int complaintId, int days)
        {
            try { 
            await complaintService.SuspendUserAsync(complaintId, days);
            return Ok(new { success = true, message = $"User suspended for {days} days." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Refuse complaint
        [HttpPut("refuse/{complaintId}")]
        public async Task<IActionResult> RefuseComplaint(int complaintId)
        {
            try { 
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
