using CampusServicesPortal.DTOs.Requests.Labs;
using CampusServicesPortal.DTOs.Responses.Labs;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Services.Implementations;

public class LabService : ILabService
{
    // Declared exactly once to resolve any compiler ambiguity errors
    private readonly ILabRepository _labRepo;

    public LabService(ILabRepository labRepo)
    {
        _labRepo = labRepo;
    }

    public async Task<bool> CreateLabAsync(string name, string labType, int capacity, int? totalRows = 4, int? totalColumns = 3)
    {
        int finalCapacity = capacity;
        if (labType.Equals("Computer", StringComparison.OrdinalIgnoreCase) && totalRows.HasValue && totalColumns.HasValue)
        {
            finalCapacity = totalRows.Value * totalColumns.Value;
        }

        var newLab = new Lab 
        { 
            Name = name, 
            LabType = labType, 
            Capacity = finalCapacity,
            TotalRows = totalRows ?? 4,
            TotalColumns = totalColumns ?? 3,
        };
        await _labRepo.AddLabAsync(newLab);
        return await _labRepo.SaveChangesAsync();
    }

    public async Task<bool> AddSeatToLabAsync(int labId, string seatNumber, int rowIndex = 1, int columnIndex = 1, string? equipmentDetails = null)
    {
        var lab = await _labRepo.GetByIdAsync(labId);
        if (lab == null)
            throw new KeyNotFoundException("Laboratory not found.");

        // Format seat number with LAB{labId}- prefix if not present
        string formattedSeatNumber = seatNumber;
        if (!seatNumber.StartsWith($"LAB{labId}-", StringComparison.OrdinalIgnoreCase))
        {
            formattedSeatNumber = $"LAB{labId}-{seatNumber}";
        }

        await _labRepo.AddSeatAsync(new LabSeat 
        { 
            LabId = labId, 
            SeatNumber = formattedSeatNumber,
            RowIndex = rowIndex,
            ColumnIndex = columnIndex,
        });
        return await _labRepo.SaveChangesAsync();
    }

    public async Task<bool> RemoveSeatFromLabAsync(int labId, int seatId)
    {
        var seats = await _labRepo.GetSeatsByLabIdAsync(labId);
        var targetSeat = seats.FirstOrDefault(s => s.Id == seatId);
        if (targetSeat == null)
            throw new KeyNotFoundException("Seat not found in this laboratory.");

        // Rule: A seat cannot be deleted while it has future active bookings
        bool hasBookings = await _labRepo.HasFutureBookingsForSeatAsync(seatId);
        if (hasBookings)
            throw new InvalidOperationException("Cannot remove workstation seat because it has pending future bookings.");

        await _labRepo.DeleteSeatAsync(targetSeat);
        return await _labRepo.SaveChangesAsync();
    }

    public async Task<IEnumerable<LabMinimalResponseDto>> GetAllLabsAsync()
    {
        var labs = await _labRepo.GetAllAsync();
        return labs.Select(l => new LabMinimalResponseDto
        {
            Id = l.Id,
            Name = l.Name,
            LabType = l.LabType,
            Capacity = l.Capacity,
            SeatsBuilt = l.Seats != null ? l.Seats.Count : 0
        });
    }

    // Time Slot Management Implementations
    public async Task<IEnumerable<LabTimeSlotResponseDto>> GetLabTimeSlotsAsync(int labId, bool activeOnly = false)
    {
        var slots = (await _labRepo.GetTimeSlotsByLabIdAsync(labId, activeOnly)).ToList();
        if (!slots.Any())
        {
            var defaultSlots = new List<LabBookingTimeSlot>
            {
                new LabBookingTimeSlot { LabId = labId, StartTime = "09:00 AM", EndTime = "11:00 AM", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new LabBookingTimeSlot { LabId = labId, StartTime = "11:00 AM", EndTime = "01:00 PM", DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                new LabBookingTimeSlot { LabId = labId, StartTime = "02:00 PM", EndTime = "04:00 PM", DisplayOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                new LabBookingTimeSlot { LabId = labId, StartTime = "04:00 PM", EndTime = "06:00 PM", DisplayOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            foreach (var s in defaultSlots)
            {
                await _labRepo.AddTimeSlotAsync(s);
            }
            await _labRepo.SaveChangesAsync();

            slots = (await _labRepo.GetTimeSlotsByLabIdAsync(labId, activeOnly)).ToList();
        }

        return slots.Select(s => new LabTimeSlotResponseDto
        {
            Id = s.Id,
            LabId = s.LabId,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            IsActive = s.IsActive,
            DisplayOrder = s.DisplayOrder,
            IsAvailable = true
        });
    }

    public async Task<IEnumerable<LabTimeSlotResponseDto>> GetAvailableTimeSlotsAsync(int labId, DateTime date)
    {
        return await GetLabTimeSlotsAsync(labId, activeOnly: true);
    }

    public async Task<LabTimeSlotResponseDto> CreateLabTimeSlotAsync(CampusServicesPortal.DTOs.Requests.Labs.CreateLabTimeSlotDto dto)
    {
        var lab = await _labRepo.GetByIdAsync(dto.LabId);
        if (lab == null) throw new KeyNotFoundException("Laboratory not found.");

        if (string.Compare(dto.EndTime, dto.StartTime, StringComparison.OrdinalIgnoreCase) <= 0)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        bool isOverlapping = await _labRepo.HasOverlappingTimeSlotAsync(dto.LabId, dto.StartTime, dto.EndTime);
        if (isOverlapping)
        {
            throw new InvalidOperationException("An active time slot with an overlapping time range already exists for this laboratory.");
        }

        var newSlot = new LabBookingTimeSlot
        {
            LabId = dto.LabId,
            StartTime = dto.StartTime.Trim(),
            EndTime = dto.EndTime.Trim(),
            DisplayOrder = dto.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _labRepo.AddTimeSlotAsync(newSlot);
        await _labRepo.SaveChangesAsync();

        return new LabTimeSlotResponseDto
        {
            Id = newSlot.Id,
            LabId = newSlot.LabId,
            StartTime = newSlot.StartTime,
            EndTime = newSlot.EndTime,
            IsActive = newSlot.IsActive,
            DisplayOrder = newSlot.DisplayOrder,
            IsAvailable = true
        };
    }

    public async Task<LabTimeSlotResponseDto> UpdateLabTimeSlotAsync(int slotId, CampusServicesPortal.DTOs.Requests.Labs.UpdateLabTimeSlotDto dto)
    {
        var slot = await _labRepo.GetTimeSlotByIdAsync(slotId);
        if (slot == null) throw new KeyNotFoundException("Time slot record not found.");

        if (string.Compare(dto.EndTime, dto.StartTime, StringComparison.OrdinalIgnoreCase) <= 0)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        bool isOverlapping = await _labRepo.HasOverlappingTimeSlotAsync(slot.LabId, dto.StartTime, dto.EndTime, slotId);
        if (isOverlapping)
        {
            throw new InvalidOperationException("An active time slot with an overlapping time range already exists for this laboratory.");
        }

        slot.StartTime = dto.StartTime.Trim();
        slot.EndTime = dto.EndTime.Trim();
        slot.DisplayOrder = dto.DisplayOrder;
        slot.IsActive = dto.IsActive;
        slot.UpdatedAt = DateTime.UtcNow;

        await _labRepo.SaveChangesAsync();

        return new LabTimeSlotResponseDto
        {
            Id = slot.Id,
            LabId = slot.LabId,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            IsActive = slot.IsActive,
            DisplayOrder = slot.DisplayOrder,
            IsAvailable = true
        };
    }

    public async Task<bool> ToggleTimeSlotActiveAsync(int slotId, bool isActive)
    {
        var slot = await _labRepo.GetTimeSlotByIdAsync(slotId);
        if (slot == null) throw new KeyNotFoundException("Time slot record not found.");

        slot.IsActive = isActive;
        slot.UpdatedAt = DateTime.UtcNow;
        return await _labRepo.SaveChangesAsync();
    }

    public async Task<bool> DeleteLabTimeSlotAsync(int slotId)
    {
        var slot = await _labRepo.GetTimeSlotByIdAsync(slotId);
        if (slot == null) throw new KeyNotFoundException("Time slot record not found.");

        bool hasBookings = await _labRepo.HasBookingsForTimeSlotAsync(slotId);
        if (hasBookings)
        {
            throw new InvalidOperationException("Cannot delete time slot because it has associated active or past bookings. Deactivate the slot instead.");
        }

        await _labRepo.DeleteTimeSlotAsync(slot);
        return await _labRepo.SaveChangesAsync();
    }
}
