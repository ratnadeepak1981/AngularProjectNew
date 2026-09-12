using CampusServicesPortal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Controllers;

[ApiController]
[Route("api/labs")]
public class LabsController : BaseApiController
{
    private readonly ILabService _labService;

    public LabsController(ILabService labService)
    {
        _labService = labService;
    }

    // GET /api/labs - Anyone authenticated can view the directory list
    [HttpGet]
    public async Task<IActionResult> GetLabs()
    {
        var labs = await _labService.GetAllLabsAsync();
        return ProcessServiceResult(ServiceResult<object>.Success(labs, 200), "Laboratories list retrieved successfully.");
    }

    // POST /api/labs - Rule 4: Admin role authorization guard enforced server-side
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateLab([FromBody] CreateLabRequest request)
    {
        var success = await _labService.CreateLabAsync(request.Name, request.LabType, request.Capacity, request.TotalRows, request.TotalColumns);
        if (!success) return BadRequest("Unable to instantiate laboratory profile.");
        return StatusCode(201); // 201 Created
    }

    // POST /api/labs/{id}/seats - Admin only layout configuration setup
    [HttpPost("{id}/seats")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AddSeat(int id, [FromBody] AddSeatRequest request)
    {
        try
        {
            var success = await _labService.AddSeatToLabAsync(id, request.SeatNumber, request.RowIndex, request.ColumnIndex, request.EquipmentDetails);
            return success ? Ok() : BadRequest("Failed to register physical workstation seat.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message); // Properly bubbles up validation rule blocks
        }
    }

    // DELETE /api/labs/{id}/seats/{seatId} - Rule 8: Intercepts if active dependencies exist
    [HttpDelete("{id}/seats/{seatId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RemoveSeat(int id, int seatId)
    {
        try
        {
            var success = await _labService.RemoveSeatFromLabAsync(id, seatId);
            return success ? Ok() : BadRequest("Unable to remove physical seat structure.");
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    // =========================================================================
    // TIME SLOT MANAGEMENT ENDPOINTS
    // =========================================================================

    // GET /api/labs/{id}/time-slots
    [HttpGet("{id}/time-slots")]
    [Authorize(Roles = "Admin,SuperAdmin,Student")]
    public async Task<IActionResult> GetTimeSlots(int id, [FromQuery] bool activeOnly = false)
    {
        var slots = await _labService.GetLabTimeSlotsAsync(id, activeOnly);
        return ProcessServiceResult(ServiceResult<object>.Success(slots, 200), "Lab time slots retrieved successfully.");
    }

    // GET /api/labs/{id}/time-slots/available?date={date}
    [HttpGet("{id}/time-slots/available")]
    [Authorize(Roles = "Admin,SuperAdmin,Student")]
    public async Task<IActionResult> GetAvailableTimeSlots(int id, [FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.Today;
        var slots = await _labService.GetAvailableTimeSlotsAsync(id, targetDate);
        return ProcessServiceResult(ServiceResult<object>.Success(slots, 200), "Available lab time slots retrieved successfully.");
    }

    // POST /api/labs/{id}/time-slots
    [HttpPost("{id}/time-slots")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateTimeSlot(int id, [FromBody] CampusServicesPortal.DTOs.Requests.Labs.CreateLabTimeSlotDto request)
    {
        try
        {
            request.LabId = id;
            var created = await _labService.CreateLabTimeSlotAsync(request);
            return StatusCode(201, created);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    // PUT /api/labs/time-slots/{slotId}
    [HttpPut("time-slots/{slotId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateTimeSlot(int slotId, [FromBody] CampusServicesPortal.DTOs.Requests.Labs.UpdateLabTimeSlotDto request)
    {
        try
        {
            var updated = await _labService.UpdateLabTimeSlotAsync(slotId, request);
            return Ok(updated);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    // PATCH /api/labs/time-slots/{slotId}/toggle-active
    [HttpPatch("time-slots/{slotId}/toggle-active")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ToggleTimeSlotActive(int slotId, [FromBody] ToggleTimeSlotActiveRequest request)
    {
        try
        {
            var success = await _labService.ToggleTimeSlotActiveAsync(slotId, request.IsActive);
            return success ? Ok(new { message = $"Time slot active status updated to {request.IsActive}." }) : BadRequest("Unable to toggle time slot active status.");
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    // DELETE /api/labs/time-slots/{slotId}
    [HttpDelete("time-slots/{slotId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteTimeSlot(int slotId)
    {
        try
        {
            var success = await _labService.DeleteLabTimeSlotAsync(slotId);
            return success ? Ok(new { message = "Time slot deleted successfully." }) : BadRequest("Unable to delete time slot.");
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}

// Request contracts scoped locally to clean up the controller signature 
public record CreateLabRequest(string Name, string LabType, int Capacity, int? TotalRows, int? TotalColumns);
public record AddSeatRequest(string SeatNumber, int RowIndex = 1, int ColumnIndex = 1, string? EquipmentDetails = null);
public record ToggleTimeSlotActiveRequest(bool IsActive);
