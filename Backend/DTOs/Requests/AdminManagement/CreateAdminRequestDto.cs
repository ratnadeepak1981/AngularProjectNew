using System.ComponentModel.DataAnnotations;

namespace CampusServicesPortal.DTOs.Requests.AdminManagement
{
    public class CreateAdminRequestDto
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;
    }
}
