using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.Models;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/venues")]
    public class VenuesController : BaseApiController
    {
        private readonly IVenueService _venueService;

        public VenuesController(IVenueService venueService)
        {
            _venueService = venueService;
        }

        // GET /api/venues - List all active venues for dropdowns and directories
        [HttpGet]
        public async Task<IActionResult> GetVenues()
        {
            var result = await _venueService.GetVenuesAsync();
            return ProcessServiceResult(result, "Active university venues retrieved successfully.");
        }

        // POST /api/venues - Admin create new Event Hall or Open Space venue (BRD Page 10)
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> CreateVenue([FromBody] CreateVenueDto request)
        {
            var result = await _venueService.CreateVenueAsync(request.Name, request.VenueType, request.Capacity);
            return ProcessServiceResult(result, "University venue created successfully.");
        }

        // PUT /api/venues/{id} - Admin amend venue details (BRD Page 10)
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateVenue(int id, [FromBody] UpdateVenueDto request)
        {
            var result = await _venueService.UpdateVenueAsync(id, request.Name, request.VenueType, request.Capacity, request.IsActive);
            return ProcessServiceResult(result, "University venue details updated successfully.");
        }

        // DELETE /api/venues/{id} - Admin soft-deactivate venue (BRD Page 10)
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            var result = await _venueService.DeleteVenueAsync(id);
            return ProcessServiceResult(result, "Venue soft-deactivated successfully.");
        }

        // GET /api/venues/{id}/availability?from=&to= - Check venue schedule overlap (BRD Page 10)
        [HttpGet("{id:int}/availability")]
        public async Task<IActionResult> CheckAvailability(int id, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var result = await _venueService.CheckAvailabilityAsync(id, from, to);
            return ProcessServiceResult(result, "Venue schedule availability checked.");
        }
    }

    public record CreateVenueDto(string Name, string VenueType, int Capacity);
    public record UpdateVenueDto(string Name, string VenueType, int Capacity, bool IsActive);
}
