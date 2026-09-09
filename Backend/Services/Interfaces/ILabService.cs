using CampusServicesPortal.DTOs.Requests.Labs;
using CampusServicesPortal.DTOs.Responses.Labs;

namespace CampusServicesPortal.Services.Interfaces;

public interface ILabService
{
    Task<IEnumerable<LabMinimalResponseDto>> GetAllLabsAsync();
    Task<bool> CreateLabAsync(string name, string labType, int capacity, int? totalRows = 4, int? totalColumns = 3);
    Task<bool> AddSeatToLabAsync(int labId, string seatNumber, int rowIndex = 1, int columnIndex = 1, string? equipmentDetails = null);
    Task<bool> RemoveSeatFromLabAsync(int labId, int seatId);

    // Time Slot Management Services
    Task<IEnumerable<LabTimeSlotResponseDto>> GetLabTimeSlotsAsync(int labId, bool activeOnly = false);
    Task<IEnumerable<LabTimeSlotResponseDto>> GetAvailableTimeSlotsAsync(int labId, DateTime date);
    Task<LabTimeSlotResponseDto> CreateLabTimeSlotAsync(CreateLabTimeSlotDto dto);
    Task<LabTimeSlotResponseDto> UpdateLabTimeSlotAsync(int slotId, UpdateLabTimeSlotDto dto);
    Task<bool> ToggleTimeSlotActiveAsync(int slotId, bool isActive);
    Task<bool> DeleteLabTimeSlotAsync(int slotId);
}
