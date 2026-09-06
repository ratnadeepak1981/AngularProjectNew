using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Labs;
using CampusServicesPortal.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CampusServicesPortal.Controllers;

[ApiController]
[Route("api/lab-bookings")]
public class LabBookingsController : BaseApiController
{
    private readonly ILabBookingService _bookingService;

    public LabBookingsController(ILabBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("student/{studentId:int}")]
    public async Task<IActionResult> GetStudentBookings(int studentId)
    {
        var userBookings = await _bookingService.GetStudentBookingsAsync(studentId);
        return ProcessServiceResult(Wrappers.ServiceResult<object>.Success(userBookings, 200), "Student lab bookings retrieved.");
    }

    [HttpGet("audit-history")]
    public async Task<IActionResult> GetAuditHistory()
    {
        var studentId = 1;
        var auditHistory = await _bookingService.GetStudentBookingsAsync(studentId);
        return ProcessServiceResult(Wrappers.ServiceResult<object>.Success(auditHistory, 200), "Lab bookings audit history retrieved.");
    }

    [HttpGet("layout/{labId:int}")]
    public async Task<IActionResult> GetLayout(int labId, [FromQuery] DateTime? date, [FromQuery] string? timeSlot)
    {
        try
        {
            var targetDate = date ?? DateTime.Today;
            var targetSlot = string.IsNullOrWhiteSpace(timeSlot) ? "09:00 - 11:00 AM" : timeSlot;
            var gridMatrix = await _bookingService.GetLabLayoutMatrixAsync(labId, targetDate, targetSlot);
            return ProcessServiceResult(Wrappers.ServiceResult<object>.Success(gridMatrix, 200), "Lab layout grid matrix retrieved.");
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> CreateHold([FromBody] CreateLabBookingDto requestDto)
    {
        try
        {
            var holdResult = await _bookingService.CreateReservationHoldAsync(requestDto);
            return ProcessServiceResult(Wrappers.ServiceResult<object>.Success(holdResult, 201), "Lab seat hold placed successfully.");
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return StatusCode(409, new { message = ex.Message, isConflict = true }); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpPut("{id:int}/confirm")]
    public async Task<IActionResult> ConfirmHold(int id)
    {
        var success = await _bookingService.ConfirmBookingAsync(id);
        if (!success) return StatusCode(409, new { message = "Lock window expired or hold reservation context missing.", isConflict = true });
        return ProcessServiceResult(Wrappers.ServiceResult<object>.Success(new { Message = "Booking confirmed." }, 200), "Lab booking confirmed.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelBooking(int id, [FromQuery] int studentId)
    {
        var success = await _bookingService.CancelBookingAsync(id, studentId);
        if (!success) return BadRequest("Cancellation rejected. Ensure ownership validation criteria match.");
        return ProcessServiceResult(Wrappers.ServiceResult<object>.Success(new { Message = "Booking cancelled." }, 200), "Lab booking cancelled.");
    }
}
