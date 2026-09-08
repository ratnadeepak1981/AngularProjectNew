using CampusServicesPortal.DTOs.Requests.Labs;
using CampusServicesPortal.DTOs.Responses.Labs;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Services.Interfaces;

public interface ILabBookingService
{
    Task<IEnumerable<LabMinimalResponseDto>> GetAllLabsAsync();
    Task<LabMatrixLayoutDto> GetLabLayoutMatrixAsync(int labId, DateTime date, string timeSlot);
    Task<IEnumerable<LabBookingResponseDto>> GetStudentBookingsAsync(int studentId);
    Task<LabBookingResponseDto> CreateReservationHoldAsync(CreateLabBookingDto requestDto);
    Task<bool> ConfirmBookingAsync(int bookingId);
    Task<bool> CancelBookingAsync(int bookingId, int studentId);

    Task ProcessExpiredHoldsAsync();

    Task<IEnumerable<LabBookingResponseDto>> GetAllBookingsAsync();

    
}




