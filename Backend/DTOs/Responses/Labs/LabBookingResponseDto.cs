using System;

namespace CampusServicesPortal.DTOs.Responses.Labs
{
    public class LabBookingResponseDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }

        public string StudentName { get; set; } = null!;
        public string LabName { get; set; } = null!;
        public string LabType { get; set; } = null!; // "Computer" or "Science"
        public string? SeatNumber { get; set; }
        public DateTime BookingDate { get; set; }
        public string TimeSlot { get; set; } = null!;
        public string Status { get; set; } = null!; // "Held", "Confirmed", "Expired", "Cancelled"
        public DateTime ExpiresAt { get; set; }
    }
}
