using System.ComponentModel.DataAnnotations;

namespace CampusServicesPortal.DTOs.Requests.AdminManagement
{
    public class ToggleAdminStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
