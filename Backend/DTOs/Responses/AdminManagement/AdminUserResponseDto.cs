using System;

namespace CampusServicesPortal.DTOs.Responses.AdminManagement
{
    public class AdminUserResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastPasswordChangedAt { get; set; }
    }
}
