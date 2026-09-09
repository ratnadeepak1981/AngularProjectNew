using System;

namespace CampusServicesPortal.DTOs.Requests.Labs;

public class CreateLabTimeSlotDto
{
    public int LabId { get; set; }
    public required string StartTime { get; set; }
    public required string EndTime { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class UpdateLabTimeSlotDto
{
    public required string StartTime { get; set; }
    public required string EndTime { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class LabTimeSlotResponseDto
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public required string StartTime { get; set; }
    public required string EndTime { get; set; }
    public string TimeSlotString => $"{StartTime} - {EndTime}";
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; } = true;
}
