using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Labs;
using CampusServicesPortal.Exceptions;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
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

   

    // =========================================================================
    // 🛠️ ARCHITECTURAL FIX: Hook into the complete global audit data channel
    // =========================================================================
    [HttpGet("audit-history")]
    public async Task<IActionResult> GetAuditHistory()
    {
        // 👇 Call the new method to pull full history for all students with names loaded
        var auditHistory = await _bookingService.GetAllBookingsAsync();

        return ProcessServiceResult(
            Wrappers.ServiceResult<object>.Success(auditHistory, 200),
            "All campus lab bookings audit history log records retrieved successfully."
        );
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
        catch (DuplicateBookingException ex) { return StatusCode(409, new ApiResponse<object>(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return StatusCode(409, new ApiResponse<object>(ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpPut("{id:int}/confirm")]
    public async Task<IActionResult> ConfirmHold(int id)
    {
        try
        {
            var success = await _bookingService.ConfirmBookingAsync(id);
            if (!success) return StatusCode(409, new ApiResponse<object>("Lock window expired or hold reservation context missing."));
            return ProcessServiceResult(Wrappers.ServiceResult<object>.Success(new { Message = "Booking confirmed." }, 200), "Lab booking confirmed.");
        }
        catch (DuplicateBookingException ex) { return StatusCode(409, new ApiResponse<object>(ex.Message)); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelBooking(int id, [FromQuery] int studentId)
    {
        var success = await _bookingService.CancelBookingAsync(id, studentId);
        if (!success) return BadRequest("Cancellation rejected. Ensure ownership validation criteria match.");
        return ProcessServiceResult(Wrappers.ServiceResult<object>.Success(new { Message = "Booking cancelled." }, 200), "Lab booking cancelled.");
    }
}
