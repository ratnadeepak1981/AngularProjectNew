using CampusServicesPortal.Data;
using CampusServicesPortal.DTOs.Requests.Labs;
using CampusServicesPortal.DTOs.Requests.Nortifcation;
using CampusServicesPortal.DTOs.Responses.Labs;
using CampusServicesPortal.Exceptions;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Implementations;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CampusServicesPortal.Services.Implementations;

public class LabBookingService : ILabBookingService
{
    private readonly ILabRepository _labRepo;
    private readonly ILabBookingRepository _bookingRepo;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly INotificationService _notificationService;

    public LabBookingService(ILabRepository labRepo, ILabBookingRepository bookingRepo, AppDbContext context, INotificationService notificationService, IConfiguration config)
    {
        _labRepo = labRepo;
        _bookingRepo = bookingRepo;
        _context = context;
        _config = config;
        _notificationService = notificationService; // Assigned service dependency
    }

    public async Task<IEnumerable<LabBookingResponseDto>> GetStudentBookingsAsync(int studentId)
    {
        var bookings = await _bookingRepo.GetStudentBookingsAsync(studentId);
        return bookings.Select(b => new LabBookingResponseDto
        {
            Id = b.Id,
            StudentId = b.StudentId,
            LabName = b.Lab?.Name ?? string.Empty,
            LabType = b.Lab?.LabType ?? string.Empty,

            // FIX: Changed from b.LabSeat?.SeatNumber to b.Seat?.SeatNumber
            SeatNumber = b.Seat?.SeatNumber,

            BookingDate = b.BookingDate,
            TimeSlot = b.TimeSlot,
            Status = b.Status,
            ExpiresAt = b.ExpiresAt
        });
    }


    // Maps directly to your updated grid layout DTO schema requirements
    public async Task<LabMatrixLayoutDto> GetLabLayoutMatrixAsync(int labId, DateTime date, string timeSlot)
    {
        var lab = await _labRepo.GetByIdAsync(labId);
        if (lab == null) throw new KeyNotFoundException("Laboratory structure not found.");

        await ProcessExpiredHoldsAsync();

        var staticSeats = await _labRepo.GetSeatsByLabIdAsync(labId);
        var activeBookings = (await _bookingRepo.GetActiveBookingsForLabSlotAsync(labId, date, timeSlot)).ToList();
        var bookingsBySeatId = activeBookings
            .Where(b => b.SeatId.HasValue)
            .GroupBy(b => b.SeatId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(b => b.Status == "Confirmed")
                      .ThenByDescending(b => b.ExpiresAt)
                      .ThenByDescending(b => b.Id)
                      .First()
            );

        var mappedSeatsList = new List<LabSeatStatusDto>();

        foreach (var seat in staticSeats)
        {
            string calculatedStatus = "Available";

            // If your physical seat model tracks maintenance/hardware faults, check it first
            if (seat.IsBroken)
            {
                calculatedStatus = "Broken";
            }
            else if (bookingsBySeatId.TryGetValue(seat.Id, out var activeBooking))
            {
                calculatedStatus = activeBooking.Status.Equals("Held", StringComparison.OrdinalIgnoreCase)
                    ? "Held"
                    : "Occupied";
            }

            mappedSeatsList.Add(new LabSeatStatusDto
            {
                Id = seat.Id,                   // Maps to your exact property field name
                SeatNumber = seat.SeatNumber,
                RowIndex = seat.RowIndex,       // Maps to your exact property field name
                ColumnIndex = seat.ColumnIndex, // Maps to your exact property field name
                Status = calculatedStatus
            });
        }

        return new LabMatrixLayoutDto
        {
            // Dynamically evaluate bounding grid size for frontend canvas render setups
            TotalRows = lab.TotalRows ?? (mappedSeatsList.Any() ? mappedSeatsList.Max(s => s.RowIndex) : 4),
            TotalColumns = lab.TotalColumns ?? (mappedSeatsList.Any() ? mappedSeatsList.Max(s => s.ColumnIndex) : 3),
            Seats = mappedSeatsList
        };
    }

    public async Task<LabBookingResponseDto> CreateReservationHoldAsync(CreateLabBookingDto requestDto)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _bookingRepo.BeginSerializableTransactionAsync(); // Rule 2
            try
            {
                var lab = await _labRepo.GetByIdAsync(requestDto.LabId);
                if (lab == null) throw new KeyNotFoundException("Laboratory record not found.");

                // Check student daily booking limit from SystemSettings
                var maxSlotSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "MaxLabBookingsPerStudentPerDay" || s.SettingKey == "MaxDailySlots");
                int maxDailySlots = maxSlotSetting != null && int.TryParse(maxSlotSetting.SettingValue, out var maxSlots) ? maxSlots : 2;

                var studentDayBookings = await _context.LabBookings
                    .CountAsync(b => b.StudentId == requestDto.StudentId
                        && b.BookingDate.Date == requestDto.BookingDate.Date
                        && (b.Status == "Confirmed" || (b.Status == "Held" && b.ExpiresAt > DateTime.UtcNow)));

                if (studentDayBookings >= maxDailySlots)
                {
                    throw new InvalidOperationException($"Daily limit reached: Maximum {maxDailySlots} slots ({maxDailySlots * 2} hours total) allowed per calendar day.");
                }

                // Check if student already holds an active or confirmed booking in this time slot
                var existingStudentBooking = await _context.LabBookings
                    .FirstOrDefaultAsync(b => b.StudentId == requestDto.StudentId
                        && b.BookingDate.Date == requestDto.BookingDate.Date
                        && b.TimeSlot == requestDto.TimeSlot
                        && (b.Status == "Confirmed" || (b.Status == "Held" && b.ExpiresAt > DateTime.UtcNow)));

                if (existingStudentBooking != null)
                {
                    // If the student clicked the exact same seat they already hold, renew the hold
                    if (requestDto.SeatId.HasValue && existingStudentBooking.SeatId == requestDto.SeatId && existingStudentBooking.Status == "Held")
                    {
                        var holdSettingRenew = await _context.SystemSettings
                            .FirstOrDefaultAsync(s => s.SettingKey == "LabBookingHoldMinutes" || s.SettingKey == "reservation-hold-minutes");
                        int renewMinutes = holdSettingRenew != null && int.TryParse(holdSettingRenew.SettingValue, out var rm) ? rm : _config.GetValue<int>("SystemSettings:ReservationHoldMinutes", 15);
                        existingStudentBooking.ExpiresAt = DateTime.UtcNow.AddMinutes(renewMinutes);
                        await _bookingRepo.SaveChangesAsync();
                        await transaction.CommitAsync();

                        var seatList = await _labRepo.GetSeatsByLabIdAsync(requestDto.LabId);
                        var s = seatList?.FirstOrDefault(x => x.Id == requestDto.SeatId);

                        return new LabBookingResponseDto
                        {
                            Id = existingStudentBooking.Id,
                            StudentId = existingStudentBooking.StudentId,
                            LabName = lab.Name,
                            LabType = lab.LabType,
                            SeatNumber = s?.SeatNumber,
                            BookingDate = existingStudentBooking.BookingDate,
                            TimeSlot = existingStudentBooking.TimeSlot,
                            Status = existingStudentBooking.Status,
                            ExpiresAt = existingStudentBooking.ExpiresAt
                        };
                    }

                    throw new DuplicateBookingException("You already hold an active booking slot for this specific date and time frame.");
                }

                if (lab.LabType.Equals("Computer", StringComparison.OrdinalIgnoreCase)) // Rule 8
                {
                    if (!requestDto.SeatId.HasValue) throw new ArgumentException("Seat selection required for Computer Labs.");

                    var activeBooking = await _bookingRepo.GetActiveBookingForSeatAsync(requestDto.LabId, requestDto.SeatId.Value, requestDto.BookingDate, requestDto.TimeSlot);
                    if (activeBooking != null)
                    {
                        throw new InvalidOperationException("The requested workstation seat is already occupied or held by another student.");
                    }
                }
                else if (lab.LabType.Equals("Science", StringComparison.OrdinalIgnoreCase))
                {
                    int activeCount = await _bookingRepo.GetActiveBookingsCountForSlotAsync(requestDto.LabId, requestDto.BookingDate, requestDto.TimeSlot);
                    if (activeCount >= lab.Capacity) throw new InvalidOperationException("The requested session slot has reached max student capacity.");
                }

                var holdSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "LabBookingHoldMinutes" || s.SettingKey == "reservation-hold-minutes");

                int holdMinutes = holdSetting != null && int.TryParse(holdSetting.SettingValue, out var parsedMins) ? parsedMins : _config.GetValue<int>("SystemSettings:ReservationHoldMinutes", 15);

                var newBooking = new LabBooking
                {
                    LabId = requestDto.LabId,
                    StudentId = requestDto.StudentId,
                    SeatId = lab.LabType.Equals("Computer", StringComparison.OrdinalIgnoreCase) ? requestDto.SeatId : null,
                    BookingDate = requestDto.BookingDate.Date,
                    TimeSlot = requestDto.TimeSlot,
                    Status = "Held", // Set short-term reservation locking state code flag (Rule 12)
                    ExpiresAt = DateTime.UtcNow.AddMinutes(holdMinutes)
                };

                await _bookingRepo.AddBookingAsync(newBooking);
                await _bookingRepo.SaveChangesAsync();
                await transaction.CommitAsync();

                var seats = requestDto.SeatId.HasValue ? await _labRepo.GetSeatsByLabIdAsync(requestDto.LabId) : null;
                var targetSeat = seats?.FirstOrDefault(s => s.Id == requestDto.SeatId);

                return new LabBookingResponseDto
                {
                    Id = newBooking.Id,
                    StudentId = newBooking.StudentId,
                    LabName = lab.Name,
                    LabType = lab.LabType,
                    SeatNumber = targetSeat?.SeatNumber,
                    BookingDate = newBooking.BookingDate,
                    TimeSlot = newBooking.TimeSlot,
                    Status = newBooking.Status,
                    ExpiresAt = newBooking.ExpiresAt
                };
            }
            catch { await transaction.RollbackAsync(); throw; }
        });
    }

    public async Task<bool> ConfirmBookingAsync(int bookingId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _bookingRepo.BeginSerializableTransactionAsync();
            try
            {
                // 1. Fetch and validate the booking reference row parameters
                var booking = await _bookingRepo.GetByIdAsync(bookingId);
                if (booking == null) return false;
                if (booking.Status == "Confirmed") return true; // Idempotent
                if (booking.Status != "Held" || booking.ExpiresAt < DateTime.UtcNow)
                    return false;

                // 2. Prevent concurrent double-booking: Check if seat is already confirmed
                if (booking.SeatId.HasValue)
                {
                    var isAlreadyConfirmed = await _context.LabBookings
                        .AnyAsync(b => b.Id != booking.Id
                            && b.LabId == booking.LabId
                            && b.SeatId == booking.SeatId
                            && b.BookingDate.Date == booking.BookingDate.Date
                            && b.TimeSlot == booking.TimeSlot
                            && b.Status == "Confirmed");

                    if (isAlreadyConfirmed)
                    {
                        booking.Status = "Expired";
                        await _bookingRepo.SaveChangesAsync();
                        await transaction.CommitAsync();
                        return false;
                    }

                    // 3. Purge/expire any other concurrent hold records on this exact seat/slot
                    var otherHolds = await _context.LabBookings
                        .Where(b => b.Id != booking.Id
                            && b.LabId == booking.LabId
                            && b.SeatId == booking.SeatId
                            && b.BookingDate.Date == booking.BookingDate.Date
                            && b.TimeSlot == booking.TimeSlot
                            && b.Status == "Held")
                        .ToListAsync();

                    foreach (var h in otherHolds)
                    {
                        h.Status = "Expired";
                    }
                }

                // 4. Stage the primary booking status change vector in context memory
                booking.Status = "Confirmed";
                await _bookingRepo.SaveChangesAsync();
                await transaction.CommitAsync();

                // 5. Automated trigger notification
                try
                {
                    string seatLabel = booking.Seat?.SeatNumber ?? (booking.SeatId.HasValue ? $"Workstation #{booking.SeatId}" : "Bench Slot");
                    await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
                    {
                        StudentId = booking.StudentId,
                        Type = "LabBookingConfirmed",
                        Message = $"Your temporary reservation request for {seatLabel} has been successfully verified and confirmed for your slot."
                    });
                }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Critical database execution fault inside lab booking confirmation route: {ex.Message}");
                return false;
            }
        });
    }


    public async Task<bool> CancelBookingAsync(int bookingId, int studentId)
    {
        var booking = await _bookingRepo.GetByIdAsync(bookingId);
        if (booking == null || booking.StudentId != studentId) return false; // Enforce owner safety constraints

        booking.Status = "Cancelled";
        return await _bookingRepo.SaveChangesAsync();
    }

    public async Task<IEnumerable<LabMinimalResponseDto>> GetAllLabsAsync()
    {
        var labs = await _labRepo.GetAllAsync();
        return labs.Select(l => new LabMinimalResponseDto
        {
            Id = l.Id,
            Name = l.Name,
            LabType = l.LabType,
            Capacity = l.Capacity
        });
    }

    public async Task ProcessExpiredHoldsAsync()
    {
        // 1. Open a transaction scope or use the context from your booking repository safely
        var expiredBookings = await _bookingRepo.GetExpiredHeldBookingsAsync();

        if (expiredBookings.Any())
        {
            foreach (var booking in expiredBookings)
            {
                booking.Status = "Expired"; // Transition state code matching Rule 12 requirements
            }
            await _bookingRepo.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<LabBookingResponseDto>> GetAllBookingsAsync()
    {
        // 1. Call the clean repository method you already created
        var bookings = await _bookingRepo.GetAllBookingsForAuditHistoryAsync();

        // 2. Transform the database rows into the clean DTO models with Student Names
        return bookings.Select(b => new LabBookingResponseDto
        {
            Id = b.Id,
            StudentId = b.StudentId,

            // 👇 Fixes the missing name tracking issue
            StudentName = b.Student?.FullName ?? $"Student #{b.StudentId}",

            LabName = b.Lab?.Name ?? string.Empty,
            LabType = b.Lab?.LabType ?? string.Empty,
            SeatNumber = b.Seat?.SeatNumber ?? "Bench Slot",
            BookingDate = b.BookingDate,
            TimeSlot = b.TimeSlot,
            Status = b.Status,
            ExpiresAt = b.ExpiresAt
        }).ToList();
    }

}
