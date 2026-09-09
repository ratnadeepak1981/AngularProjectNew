using System.ComponentModel.DataAnnotations;

namespace CampusServicesPortal.Models;

public class Lab
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    // Valid structural types from BRD: "Computer" or "Science"
    [Required]
    [MaxLength(20)]
    public required string LabType { get; set; }

    [Required]
    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    // Grid boundary limits for the frontend designer map
    public int? TotalRows { get; set; }    // e.g., Max rows deep
    public int? TotalColumns { get; set; } // e.g., Max columns wide

    // Navigation property for Entity Framework Core relationship mapping
    public ICollection<LabSeat> Seats { get; set; } = new List<LabSeat>();
    public ICollection<LabBookingTimeSlot> TimeSlots { get; set; } = new List<LabBookingTimeSlot>();
}
