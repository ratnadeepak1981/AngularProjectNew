using CampusServicesPortal.DTOs.Responses.Student;

namespace CampusServicesPortal.DTOs.Responses.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string Role { get; set; } = null!; // "Student" or "Admin"
        public StudentProfileResponseDto Profile { get; set; } = null!;
        public bool MustChangePassword { get; set; } = false;
        public bool PasswordExpired { get; set; } = false;
        public string? ForceChangeReason { get; set; }
    }
}
