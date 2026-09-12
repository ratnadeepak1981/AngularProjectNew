using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Responses.Hostel;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.DTOs.Requests.Hostel.Application;

namespace CampusServicesPortal.Controllers
{
    [Authorize]
    [Route("api/hostel-applications")]
    public class HostelApplicationsController : BaseApiController
    {
        private readonly IHostelService _hostelService;

        public HostelApplicationsController(IHostelService hostelService)
        {
            _hostelService = hostelService;
        }

        // 🛠️ NEW ENDPOINT: GET /api/hostel-applications/hostels — Student/Admin: Fetch active master buildings lookup
        [HttpGet("hostels")]
        public async Task<IActionResult> GetHostelsMasterList()
        {
            var result = await _hostelService.GetAllActiveHostelsAsync();
            return ProcessServiceResult(result, "Master hostel buildings list loaded successfully.");
        }

        // POST /api/hostel-applications — Student: Submit a new housing application request [PDF: 0.1.6]
        [HttpPost]
        public async Task<IActionResult> SubmitApplication([FromBody] SubmitHostelApplicationDto request)
        {
            int studentId = GetCurrentStudentId();
            var result = await _hostelService.SubmitApplicationAsync(studentId, request);
            return ProcessServiceResult(result, "Hostel accommodation application profiles registered successfully.");
        }

        // PUT /api/hostel-applications/{id}/status — Admin: Approve or reject an allocation request [PDF: 0.1.7, 0.1.18]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateHostelStatusDto request)
        {
            var result = await _hostelService.UpdateStatusAsync(id, request);
            return ProcessServiceResult(result, "Hostel application triage parameters updated successfully.");
        }

        // PUT /api/hostel-applications/{id}/assign-room — Admin: Allocate a room number (Rule #3) [PDF: 0.1.7, 0.1.18]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id}/assign-room")]
        public async Task<IActionResult> AssignRoom(int id, [FromBody] AssignRoomDto request)
        {
            var result = await _hostelService.AssignRoomAsync(id, request);
            return ProcessServiceResult(result, "Hostel residential block room assigned successfully.");
        }

        // GET /api/hostel-applications/student — Student: Fetch own accommodation history [PDF: 0.1.7]
        [HttpGet("student")]
        public async Task<IActionResult> GetMyApplications()
        {
            int studentId = GetCurrentStudentId();
            var result = await _hostelService.GetStudentApplicationsAsync(studentId);
            return ProcessServiceResult(result, "Student historical housing logs compiled successfully.");
        }

        // GET /api/hostel-applications/pending — Admin: Review outstanding workloads [PDF: 0.1.7]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingApplications()
        {
            var result = await _hostelService.GetAllPendingApplicationsAsync();
            return ProcessServiceResult(result, "Unprocessed hostel workflows aggregated successfully.");
        }

        // GET /api/hostel-applications/all or GET /api/hostel-applications — Admin: Fetch all applications [PDF: 0.1.7]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllApplications()
        {
            var result = await _hostelService.GetAllApplicationsAsync();
            return ProcessServiceResult(result, "All hostel application records retrieved successfully.");
        }

        /// <summary>
        /// Safely extracts the authenticated student's unique database identity from JWT claims.
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
