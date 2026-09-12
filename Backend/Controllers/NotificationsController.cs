using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Authorize]
    [Route("api/notifications")]
    public class NotificationsController : BaseApiController
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET /api/notifications/admin-audit-log — Admin-only system notifications audit log
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("admin-audit-log")]
        public async Task<IActionResult> GetAllNotifications()
        {
            try
            {
                // Consumes the application service layer architecture cleanly
                var auditLog = await _notificationService.GetAllNotificationsAsync();
                return Ok(auditLog);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Internal database logging failure: {ex.Message}");
            }
        }

        // GET /api/notifications/student/{studentId} — List a student's notifications, newest first [PDF: 0.1.16]
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetMyNotifications(int studentId)
        {
            int currentUserId = GetCurrentStudentId();
            int targetId = studentId == 0 ? currentUserId : studentId;

            // Security Rule: Students cannot look at other students' transaction lines [PDF: 0.1.16]
            if (User.IsInRole("Student") && targetId != currentUserId)
                return Forbid("Access Denied. You are not authorized to view notifications for another profile account.");

            var result = await _notificationService.GetStudentNotificationsAsync(targetId);
            return ProcessServiceResult(result, "Notifications collection compiled and retrieved successfully.");
        }

        // PUT /api/notifications/{id}/read — Mark a notification as read (Student recipient only)
        [Authorize(Roles = "Student")]
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            int studentId = GetCurrentStudentId();
            var result = await _notificationService.MarkNotificationAsReadAsync(id, studentId);
            return ProcessServiceResult(result, "Notification status flag flipped to read successfully.");
        }

        private int GetCurrentStudentId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) throw new System.UnauthorizedAccessException("Identity profile parameters missing.");
            return int.Parse(claim.Value);
        }
    }
}
