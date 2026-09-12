using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.Certificates;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Authorize] // Enforces authentication rules globally across all handlers [PDF: 0.1.2]
    [ApiController]
    [Route("api/certificate-requests")]
    public class CertificateRequestsController : BaseApiController
    {
        private readonly ICertificateService _certificateService;

        public CertificateRequestsController(ICertificateService certificateService)
        {
            _certificateService = certificateService;
        }

        // POST /api/certificate-requests — Student: Submit a new dynamic request safely [PDF: 0.1.6]
        [HttpPost]
        public async Task<IActionResult> SubmitCertificateRequest([FromBody] SubmitCertificateRequestDto request)
        {
            // 🛠️ FIXED: Safely pulls student primary key straight out of verified token context [INDEX]
            int studentId = GetCurrentStudentId();

            var result = await _certificateService.RequestCertificateAsync(studentId, request);
            return ProcessServiceResult(result, "Certificate request submitted successfully.");
        }

        // GET /api/certificate-requests/student — Student: Fetch own specific tracking logs [PDF: 0.1.7]
        [HttpGet("student")]
        public async Task<IActionResult> GetMyCertificateRequests()
        {
            // 🛠️ FIXED: Replaces explicit parameters with bulletproof context claim validation [INDEX]
            int studentId = GetCurrentStudentId();

            var result = await _certificateService.GetStudentRequestsAsync(studentId);
            return ProcessServiceResult(result, "Student certificate requests retrieved successfully.");
        }

        // GET /api/certificate-requests — Admin: Review structural workflows [PDF: 0.1.7]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetRequests([FromQuery] string? status)
        {
            var result = await _certificateService.GetRequestsAsync(status);
            return ProcessServiceResult(result, "Certificate requests retrieved successfully.");
        }

        // PUT /api/certificate-requests/{id}/status — Admin: Approve/Reject document pipelines [PDF: 0.1.7]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateRequestStatus(int id, [FromBody] UpdateCertificateStatusDto request)
        {
            var result = await _certificateService.UpdateRequestStatusAsync(id, request);
            return ProcessServiceResult(result, "Certificate request status updated successfully.");
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
