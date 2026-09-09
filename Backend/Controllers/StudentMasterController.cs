using CampusServicesPortal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CampusServicesPortal.Controllers
{
    [ApiController]
    [Route("api/student-master")]
    public class StudentMasterController : BaseApiController
    {
        private readonly IStudentService _studentService;

        public StudentMasterController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        // GET /api/student-master/verify/{indexNumber} or GET /api/student-master/{indexNumber} [BRD Page 4]
        [HttpGet("verify/{indexNumber}")]
        [HttpGet("{indexNumber}")]
        public async Task<IActionResult> VerifyIndexNumber(string indexNumber)
        {
            if (string.Equals(indexNumber, "import", System.StringComparison.OrdinalIgnoreCase))
            {
                return NotFound("Invalid endpoint context.");
            }

            var result = await _studentService.VerifyMasterIndexAsync(indexNumber);
            return ProcessServiceResult(result, "Student master index verification successful.");
        }

        // GET /api/student-master?search= [BRD Page 4]
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchMasterList([FromQuery] string? search)
        {
            var result = await _studentService.SearchMasterRecordsAsync(search);
            return ProcessServiceResult(result, "Master record database entries retrieved successfully.");
        }

        // POST /api/student-master/import [BRD Page 4]
        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkImportMasterList(IFormFile file)
        {
            var result = await _studentService.BulkImportMasterRecordsAsync(file);
            return ProcessServiceResult(result, $"Successfully imported {result.Data} student master records.");
        }
    }
}
