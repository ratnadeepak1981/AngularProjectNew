using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.Complaints;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Authorize] // Enforces authentication rules globally across all handlers [PDF: 0.1.2]
    [ApiController]
    [Route("api/complaints")]
    public class ComplaintsController : BaseApiController
    {
        private readonly IComplaintService _complaintService;

        public ComplaintsController(IComplaintService complaintService)
        {
            _complaintService = complaintService;
        }

        // POST /api/complaints — Student: Submit a new complaint securely [PDF: 0.1.6]
        [HttpPost]
        public async Task<IActionResult> SubmitComplaint([FromBody] SubmitComplaintDto request)
        {
            // 🛠️ FIXED: Safely pulls student primary key straight out of verified token context [INDEX]
            int studentId = GetCurrentStudentId();

            var result = await _complaintService.SubmitComplaintAsync(studentId, request);
            return ProcessServiceResult(result, "Complaint submitted successfully.");
        }

        // GET /api/complaints/student — Student: Fetch own complaints ledger securely [PDF: 0.1.7]
        [HttpGet("student")]
        public async Task<IActionResult> GetMyComplaints()
        {
            // 🛠️ FIXED: Replaces explicit parameters with context claim validation [INDEX]
            int studentId = GetCurrentStudentId();

            var result = await _complaintService.GetStudentComplaintsAsync(studentId);
            return ProcessServiceResult(result, "Student complaints retrieved successfully.");
        }

        // GET /api/complaints — Admin: Review structural workflows [PDF: 0.1.7]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetComplaints([FromQuery] string? status)
        {
            var result = await _complaintService.GetComplaintsAsync(status);
            return ProcessServiceResult(result, "Complaints retrieved successfully.");
        }

        // PUT /api/complaints/{id}/status — Admin: Update grievance statuses [PDF: 0.1.7]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateComplaintStatus(int id, [FromBody] UpdateComplaintStatusDto request)
        {
            var result = await _complaintService.UpdateComplaintStatusAsync(id, request);
            return ProcessServiceResult(result, "Complaint status updated successfully.");
        }

        /// <summary>
        /// Safely extracts the authenticated student's unique database identity from JWT claims matrix [INDEX].
        /// </summary>
        private int GetCurrentStudentId()
        {
            var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(nameIdentifierClaim) || !int.TryParse(nameIdentifierClaim, out int studentId))
            {
                var subClaim = User.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(subClaim) && int.TryParse(subClaim, out studentId))
                {
                    return studentId;
                }
                throw new UnauthorizedAccessException("Identity verification failed. Invalid student token context.");
            }
            return studentId;
        }
    }
}
