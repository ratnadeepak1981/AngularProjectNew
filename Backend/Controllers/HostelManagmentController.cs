using CampusServicesPortal.DTOs.Requests.Hostel;
using CampusServicesPortal.DTOs.Requests.Hostel.Managment;
using CampusServicesPortal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CampusServicesPortal.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Route("api")]
    public class HostelManagmentController : BaseApiController
    {
        private readonly IHostelManagementService _service;

        public HostelManagmentController(IHostelManagementService service)
        {
            _service = service;
        }

        [HttpPost("hostels")]
        public async Task<IActionResult> CreateHostel([FromBody] CreateHostelRequestDto request)
        {
            var result = await _service.CreateHostelAsync(request);
            return ProcessServiceResult(result, "Hostel record created successfully.");
        }

        [HttpPut("hostels/{id}")]
        public async Task<IActionResult> UpdateHostel(int id, [FromBody] UpdateHostelRequestDto request)
        {
            var result = await _service.UpdateHostelAsync(id, request);
            return ProcessServiceResult(result, "Hostel properties modified successfully.");
        }

        [HttpDelete("hostels/{id}")]
        public async Task<IActionResult> DeleteHostel(int id)
        {
            var result = await _service.DeleteHostelAsync(id);
            return ProcessServiceResult(result, "Hostel deactivation processed successfully.");
        }

        [HttpPost("hostels/{hostelId}/rooms")]
        public async Task<IActionResult> CreateRoom(int hostelId, [FromBody] CreateRoomRequestDto request)
        {
            var result = await _service.CreateRoomAsync(hostelId, request);
            return ProcessServiceResult(result, "Hostel room unit added successfully.");
        }

        [HttpPut("rooms/{id}")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequestDto request)
        {
            var result = await _service.UpdateRoomAsync(id, request);
            return ProcessServiceResult(result, "Hostel room properties modified successfully.");
        }

        [HttpDelete("rooms/{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var result = await _service.DeleteRoomAsync(id);
            return ProcessServiceResult(result, "Hostel room unit deactivated successfully.");
        }

        [AllowAnonymous] // Rule: View current metrics dashboard tracking allocations [PDF: 0.1.7]
        [HttpGet("rooms/{id}/occupancy")]
        public async Task<IActionResult> GetOccupancy(int id)
        {
            var result = await _service.GetRoomOccupancyAsync(id);
            return ProcessServiceResult(result, "Room assignment occupancy metrics calculated successfully.");
        }
    }
}
