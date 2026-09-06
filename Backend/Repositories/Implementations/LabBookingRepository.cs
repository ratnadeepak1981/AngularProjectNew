using System.Data;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CampusServicesPortal.Repositories.Implementations;

public class LabBookingRepository : ILabBookingRepository
{
    private readonly AppDbContext _context;

    public LabBookingRepository(AppDbContext context)
    {
        _context =context;
    }

    public async Task<LabBooking?> GetByIdAsync(int id) => 
        await _context.LabBookings.Include(b => b.Lab).Include(b => b.Seat).FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IEnumerable<LabBooking>> GetStudentBookingsAsync(int studentId) =>
        await _context.LabBookings
            .Include(b => b.Lab)
            .Include(b => b.Seat)
            .Where(b => b.StudentId == studentId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

    // Counts both confirmed and active held states against total capacity (Science Labs)
    public async Task<int> GetActiveBookingsCountForSlotAsync(int labId, DateTime date, string timeSlot) =>
        await _context.LabBookings.CountAsync(b => b.LabId == labId 
            && b.BookingDate.Date == date.Date 
            && b.TimeSlot == timeSlot 
            && (b.Status == "Confirmed" || (b.Status == "Held" && b.ExpiresAt > DateTime.UtcNow)));

    // Checks specific seat layout maps (Computer Labs)
    public async Task<bool> IsSeatOccupiedOrHeldAsync(int labId, int seatId, DateTime date, string timeSlot) =>
        await _context.LabBookings.AnyAsync(b => b.LabId == labId 
            && b.SeatId == seatId 
            && b.BookingDate.Date == date.Date 
            && b.TimeSlot == timeSlot 
            && (b.Status == "Confirmed" || (b.Status == "Held" && b.ExpiresAt > DateTime.UtcNow)));

    public async Task AddBookingAsync(LabBooking booking) => 
        await _context.LabBookings.AddAsync(booking);

    // Enforces concurrency validation inside an explicit transaction block (Rule 2)
    public async Task<IDbContextTransaction> BeginSerializableTransactionAsync() =>
        await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

    public async Task<bool> SaveChangesAsync() => 
        await _context.SaveChangesAsync() > 0;

    public async Task<LabBooking?> GetActiveBookingForSeatAsync(int labId, int seatId, DateTime date, string timeSlot)
    {
        return await _context.LabBookings
            .FirstOrDefaultAsync(b => b.LabId == labId
                && b.SeatId == seatId
                && b.BookingDate.Date == date.Date
                && b.TimeSlot == timeSlot
                && (b.Status == "Confirmed" || (b.Status == "Held" && b.ExpiresAt > DateTime.UtcNow)));
    }

    public async Task<IEnumerable<LabBooking>> GetActiveBookingsForLabSlotAsync(int labId, DateTime date, string timeSlot)
    {
        return await _context.LabBookings
            .Where(b => b.LabId == labId
                && b.BookingDate.Date == date.Date
                && b.TimeSlot == timeSlot
                && (b.Status == "Confirmed" || (b.Status == "Held" && b.ExpiresAt > DateTime.UtcNow)))
            .ToListAsync();
    }

    public async Task<IEnumerable<LabBooking>> GetExpiredHeldBookingsAsync()
    {
        return await _context.LabBookings
            .Where(b => b.Status == "Held" && b.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();
    }
}
