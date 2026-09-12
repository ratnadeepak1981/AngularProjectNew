using CampusServicesPortal.Models; // Replace with your actual entity namespace

namespace CampusServicesPortal.Repositories.Interfaces;

public interface ILabRepository
{
    Task<Lab?> GetByIdAsync(int id);
    Task<IEnumerable<Lab>> GetAllAsync();
    Task<IEnumerable<LabSeat>> GetSeatsByLabIdAsync(int labId);
    Task<bool> HasFutureBookingsForSeatAsync(int seatId);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task AddLabAsync(Lab lab);
    Task AddSeatAsync(LabSeat seat);
    Task DeleteSeatAsync(LabSeat seat);

    // Time Slot Repository Methods
    Task<IEnumerable<LabBookingTimeSlot>> GetTimeSlotsByLabIdAsync(int labId, bool activeOnly = false);
    Task<LabBookingTimeSlot?> GetTimeSlotByIdAsync(int slotId);
    Task<bool> HasOverlappingTimeSlotAsync(int labId, string startTime, string endTime, int? excludeSlotId = null);
    Task<bool> HasBookingsForTimeSlotAsync(int slotId);
    Task AddTimeSlotAsync(LabBookingTimeSlot slot);
    Task DeleteTimeSlotAsync(LabBookingTimeSlot slot);

    Task<bool> SaveChangesAsync();
}
