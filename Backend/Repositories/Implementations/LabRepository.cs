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
        return activeSlots.Any(t =>
            string.Compare(startTime, t.EndTime, StringComparison.OrdinalIgnoreCase) < 0 &&
            string.Compare(endTime, t.StartTime, StringComparison.OrdinalIgnoreCase) > 0);
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
