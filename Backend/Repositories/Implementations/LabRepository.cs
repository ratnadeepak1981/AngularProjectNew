using CampusServicesPortal.Data; // Replace with your DbContext namespace
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusServicesPortal.Repositories.Implementations;

public class LabRepository : ILabRepository
{
    private readonly AppDbContext _context;

    public LabRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Lab?> GetByIdAsync(int id) =>
        await _context.Labs.FindAsync(id);

    public async Task<IEnumerable<Lab>> GetAllAsync() =>
        await _context.Labs.Include(l => l.Seats).AsNoTracking().ToListAsync();

    public async Task<IEnumerable<LabSeat>> GetSeatsByLabIdAsync(int labId) =>
        await _context.LabSeats.Where(s => s.LabId == labId).ToListAsync();

    public async Task<bool> HasFutureBookingsForSeatAsync(int seatId) =>
        await _context.LabBookings.AnyAsync(b => b.SeatId == seatId
            && b.BookingDate >= DateTime.Today
            && (b.Status == "Confirmed" || b.Status == "Held"));

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        string cleanName = name.Trim().ToLower();
        var query = _context.Labs.Where(l => l.Name.ToLower() == cleanName);
        if (excludeId.HasValue) query = query.Where(l => l.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task AddLabAsync(Lab lab) =>
        await _context.Labs.AddAsync(lab);

    public async Task AddSeatAsync(LabSeat seat) =>
        await _context.LabSeats.AddAsync(seat);

    public async Task DeleteSeatAsync(LabSeat seat) =>
        _context.LabSeats.Remove(seat);

    // Time Slot Repository Implementations
    public async Task<IEnumerable<LabBookingTimeSlot>> GetTimeSlotsByLabIdAsync(int labId, bool activeOnly = false)
    {
        var query = _context.LabBookingTimeSlots.Where(t => t.LabId == labId);
        if (activeOnly)
        {
            query = query.Where(t => t.IsActive);
        }
        return await query.OrderBy(t => t.DisplayOrder).ThenBy(t => t.StartTime).ToListAsync();
    }

    public async Task<LabBookingTimeSlot?> GetTimeSlotByIdAsync(int slotId) =>
        await _context.LabBookingTimeSlots.FindAsync(slotId);

    public async Task<bool> HasOverlappingTimeSlotAsync(int labId, string startTime, string endTime, int? excludeSlotId = null)
    {
        var query = _context.LabBookingTimeSlots.Where(t => t.LabId == labId && t.IsActive);
        if (excludeSlotId.HasValue)
        {
            query = query.Where(t => t.Id != excludeSlotId.Value);
        }

        var activeSlots = await query.ToListAsync();
        var newStartTs = ParseTimeSpan(startTime);
        var newEndTs = ParseTimeSpan(endTime);

        return activeSlots.Any(t =>
        {
            var existingStartTs = ParseTimeSpan(t.StartTime);
            var existingEndTs = ParseTimeSpan(t.EndTime);
            return newStartTs < existingEndTs && newEndTs > existingStartTs;
        });
    }

    private static TimeSpan ParseTimeSpan(string timeStr)
    {
        if (string.IsNullOrWhiteSpace(timeStr)) return TimeSpan.Zero;
        timeStr = timeStr.Trim();
        if (DateTime.TryParse(timeStr, out var dt)) return dt.TimeOfDay;
        if (TimeSpan.TryParse(timeStr, out var ts)) return ts;
        return TimeSpan.Zero;
    }

    public async Task<bool> HasBookingsForTimeSlotAsync(int slotId) =>
        await _context.LabBookings.AnyAsync(b => b.TimeSlotId == slotId && (b.Status == "Confirmed" || b.Status == "Held"));

    public async Task AddTimeSlotAsync(LabBookingTimeSlot slot) =>
        await _context.LabBookingTimeSlots.AddAsync(slot);

    public async Task DeleteTimeSlotAsync(LabBookingTimeSlot slot) =>
        _context.LabBookingTimeSlots.Remove(slot);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
