using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusServicesPortal.Models;

public class LabBookingTimeSlot
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int LabId { get; set; }

    [Required]
    [MaxLength(10)]
    public required string StartTime { get; set; } // e.g., "09:00"

    [Required]
    [MaxLength(10)]
    public required string EndTime { get; set; } // e.g., "11:00"

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(LabId))]
    public Lab? Lab { get; set; }
}
