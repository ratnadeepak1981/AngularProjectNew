using System;

namespace CampusServicesPortal.Models
{
    public class NotificationWithStudent
    {
        public int Id { get; set; }
        public string IndexNumber { get; set; } = string.Empty; // Holds the string index number from the Students table
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
