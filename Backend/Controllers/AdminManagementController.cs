using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.AdminManagement;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    [ApiController]
    [Route("api/admin-management")]
    public class AdminManagementController : BaseApiController
    {
        private readonly IAdminManagementService _adminService;

        public AdminManagementController(IAdminManagementService adminService)
        {
            _adminService = adminService;
        }

        // GET /api/admin-management/admins — SuperAdmin Only: List standard admins
        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins()
        {
            var result = await _adminService.GetAdminsAsync();
            return ProcessServiceResult(result, "Admin accounts retrieved successfully.");
        }

        // POST /api/admin-management/admins — SuperAdmin Only: Create new admin account
        [HttpPost("admins")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequestDto request)
        {
            var result = await _adminService.CreateAdminAsync(request, GetCurrentUserId());
            return ProcessServiceResult(result, "New Admin user account created successfully.");
        }

        // PUT /api/admin-management/admins/{id}/status — SuperAdmin Only: Toggle active status
        [HttpPut("admins/{id:int}/status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] ToggleAdminStatusDto request)
        {
            var result = await _adminService.ToggleAdminStatusAsync(id, request.IsActive, GetCurrentUserId());
            return ProcessServiceResult(result, "Admin account status updated successfully.");
        }

        // DELETE /api/admin-management/admins/{id} — SuperAdmin Only: Delete admin account
        [HttpDelete("admins/{id:int}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var result = await _adminService.DeleteAdminAsync(id, GetCurrentUserId());
            return ProcessServiceResult(result, "Admin account deleted successfully.");
        }

        // POST /api/admin-management/admins/{id}/reset-password — SuperAdmin Only: Reset admin password / unlock account
        [HttpPost("admins/{id:int}/reset-password")]
        public async Task<IActionResult> ResetAdminPassword(int id)
        {
            var result = await _adminService.ResetAdminPasswordAsync(id, GetCurrentUserId());
            return ProcessServiceResult(result, "Admin account password reset initiated.");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int userId) ? userId : 0;
        }
    }
}
