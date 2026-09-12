using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.AuditLogs;
using CampusServicesPortal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusServicesPortal.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Route("api/admin/audit-logs")]
    public class AuditLogsController : BaseApiController
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        // GET: /api/admin/audit-logs
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterDto filter)
        {
            var result = await _auditLogService.GetAuditLogsAsync(filter);
            return ProcessServiceResult(result, "Audit logs retrieved successfully.");
        }

        // GET: /api/admin/audit-logs/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuditLogById(long id)
        {
            var result = await _auditLogService.GetAuditLogByIdAsync(id);
            return ProcessServiceResult(result, "Audit log details retrieved successfully.");
        }

        // PUT: /api/admin/audit-logs/{id}/acknowledge
        [HttpPut("{id}/acknowledge")]
        public async Task<IActionResult> AcknowledgeLog(long id)
        {
            var adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? User.Identity?.Name
                ?? "Administrator";

            var result = await _auditLogService.MarkAsReviewedAsync(id, adminEmail);
            return ProcessServiceResult(result, "Security incident acknowledged successfully.");
        }

        // PUT: /api/admin/audit-logs/acknowledge-all
        [HttpPut("acknowledge-all")]
        public async Task<IActionResult> AcknowledgeAll()
        {
            var adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? User.Identity?.Name
                ?? "Administrator";

            var result = await _auditLogService.MarkAllAsReviewedAsync(adminEmail);
            return ProcessServiceResult(result, "All security incidents acknowledged successfully.");
        }
    }
}
