using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusServicesPortal.Models;

public class LabBooking
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int LabId { get; set; }

    [Required]
    public int StudentId { get; set; }

    // Nullable because it is only populated for "Computer" style layout bookings
    public int? SeatId { get; set; }

    [Required]
    public DateTime BookingDate { get; set; }

    public int? TimeSlotId { get; set; }

    [Required]
    [MaxLength(50)]
    public required string TimeSlot { get; set; }

    // Reservation state status flags: Held -> Confirmed (or drops to Expired/Cancelled)
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Held";

    // Used by background worker tasks to automatically drop abandoned hold selections
    [Required]
    public DateTime ExpiresAt { get; set; }

    // Relational navigation references
    [ForeignKey(nameof(LabId))]
    public Lab? Lab { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(SeatId))]
    public LabSeat? Seat { get; set; }

    [ForeignKey(nameof(TimeSlotId))]
    public LabBookingTimeSlot? BookingTimeSlot { get; set; }
}
