using CampusServicesPortal.DTOs.Requests.Student;
using CampusServicesPortal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CampusServicesPortal.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : BaseApiController
    {
        private readonly IStudentService _studentService;
        private readonly IPasswordService _passwordService;

        public StudentsController(IStudentService studentService, IPasswordService passwordService)
        {
            _studentService = studentService;
            _passwordService = passwordService;
        }

        // POST /api/students/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterStudentRequestDto request)
        {
            var result = await _studentService.RegisterStudentAsync(request);
            return ProcessServiceResult(result, "Student profile registration completed successfully.");
        }

        // GET /api/students/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentProfile(int id)
        {
            var result = await _studentService.GetStudentProfileAsync(id);
            return ProcessServiceResult(result, "Student profile data retrieved successfully.");
        }

        // PUT /api/students/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudentProfile(int id, [FromBody] UpdateStudentProfileDto request)
        {
            var result = await _studentService.UpdateStudentProfileAsync(id, request);
            return ProcessServiceResult(result, "Student profile data updated successfully.");
        }
      
        // GET /api/students?search=&faculty=
        [Authorize(Roles = "Admin,SuperAdmin")] // Rule 4: Admin role authorization required [Index 0.1.18]
        [HttpGet]
        public async Task<IActionResult> SearchStudents([FromQuery] string? search, [FromQuery] string? faculty)
        {
            var result = await _studentService.SearchStudentsAsync(search, faculty);
            return ProcessServiceResult(result, "Filtered student records aggregated successfully.");
        }

        // DELETE /api/students/{id}
        [Authorize(Roles = "Admin,SuperAdmin")] // Rule 4: Only administrators can deactivate student accounts [Index 0.1.5, 0.1.18]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeactivateStudent(int id)
        {
            // Rule 11: Performs cross-turn module integrity checks before executing deactivation [Index 0.1.5, 0.1.19]
            var result = await _studentService.DeactivateStudentAsync(id);
            return ProcessServiceResult(result, "Student profile state deactivated successfully.");
        }

        // POST /api/students/{id}/reset-password
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetStudentPassword(int id)
        {
            var result = await _passwordService.AdminResetStudentPasswordAsync(id);
            return ProcessServiceResult(result, "Student account password reset initiated.");
        }
    }
}
