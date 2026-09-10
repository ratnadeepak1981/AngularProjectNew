using System;

namespace CampusServicesPortal.DTOs.Responses.Notifications
{
    public class AdminNotificationResponseDto
    {
        public int Id { get; set; }
        public string IndexNumber { get; set; } = string.Empty; // Client UI will read this string instead of a numeric student ID
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
