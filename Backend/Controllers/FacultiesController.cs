using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.MasterData;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Authorize]
    [Route("api/faculties")]
    public class FacultiesController : BaseApiController
    {
        private readonly IFacultyService _masterDataService;

        public FacultiesController(IFacultyService masterDataService)
        {
            _masterDataService = masterDataService;
        }

        // GET /api/faculties — Fetch all university faculties [PDF: 0.1.17]
        [Authorize(Roles = "Admin,Student")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _masterDataService.GetAllFacultiesAsync();
            return ProcessServiceResult(result, "Faculties catalog retrieved successfully.");
        }

        // POST /api/faculties — Admin Only: Register a fresh academic faculty [PDF: 0.1.17]
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFacultyRequestDto request)
        {
            var result = await _masterDataService.CreateFacultyAsync(request);
            return ProcessServiceResult(result, "Faculty lookup entry initialized successfully.");
        }

        // PUT /api/faculties/{id} — Admin Only: Modify descriptors or toggle activity status [PDF: 0.1.17]
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateFacultyRequestDto request)
        {
            var result = await _masterDataService.UpdateFacultyAsync(id, request);
            return ProcessServiceResult(result, "Faculty properties modified successfully.");
        }

        // DELETE /api/faculties/{id} — Admin Only: Enforces student dependency blocks before soft-deactivating [PDF: 0.1.17]
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _masterDataService.DeleteFacultyAsync(id);
            return ProcessServiceResult(result, "Faculty deactivation cycle finalized successfully.");
        }
    }
}
