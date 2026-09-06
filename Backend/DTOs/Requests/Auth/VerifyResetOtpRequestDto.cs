using System.ComponentModel.DataAnnotations;

namespace CampusServicesPortal.DTOs.Requests.Auth
{
    public class VerifyResetOtpRequestDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(6)]
        public string OtpCode { get; set; } = string.Empty;
    }
}
