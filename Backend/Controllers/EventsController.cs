using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.Events;
using CampusServicesPortal.DTOs.Responses.Events;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Authorize]
    [Route("api/events")]
    public class EventsController : BaseApiController
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        // GET /api/events — List upcoming active schedules
        [HttpGet]
        [AllowAnonymous] // Allows prospective students to browse listings anonymously
        public async Task<IActionResult> GetAvailableEvents()
        {
            int? studentId = null;
            try
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    studentId = GetCurrentStudentId();
                }
            }
            catch { }

            var result = await _eventService.GetAvailableEventsAsync(studentId);
            return ProcessServiceResult(result, "Upcoming active event matrix aggregated successfully.");
        }

        // POST /api/events — Admin: Schedule an event with layout overlap protections (Rule #6 & #13)
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto request)
        {
            var result = await _eventService.CreateEventAsync(request);
            return ProcessServiceResult(result, "Event structure registered and published successfully.");
        }

        // POST /api/events/register — Student: Sign up for a session block (Rule #1 & #7)
        [HttpPost("register")]
        public async Task<IActionResult> RegisterForEvent([FromBody] RegisterEventDto request)
        {
            int studentId = GetCurrentStudentId();
            var result = await _eventService.RegisterForEventAsync(studentId, request);
            return ProcessServiceResult(result, "Student seat reservation signed up completed successfully.");
        }

        // DELETE /api/events/{id}/register — Release a seat allocation cleanly
        [HttpDelete("{id}/register")]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            int studentId = GetCurrentStudentId();
            var result = await _eventService.CancelRegistrationAsync(id, studentId);
            return ProcessServiceResult(result, "Event tracking registration allocation slot cancelled successfully.");
        }

        // GET /api/events/{id}/registrations — Admin: View student registrations for an event
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("{id}/registrations")]
        public async Task<IActionResult> GetEventRegistrations(int id)
        {
            var result = await _eventService.GetEventRegistrationsAsync(id);
            return ProcessServiceResult(result, "Event attendee registrations retrieved successfully.");
        }

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
