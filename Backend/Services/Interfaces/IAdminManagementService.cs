using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.AdminManagement;
using CampusServicesPortal.DTOs.Responses.AdminManagement;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Interfaces
{
    public interface IAdminManagementService
    {
        Task<ServiceResult<IEnumerable<AdminUserResponseDto>>> GetAdminsAsync();
        Task<ServiceResult<AdminUserResponseDto>> CreateAdminAsync(CreateAdminRequestDto request, int currentUserId);
        Task<ServiceResult<AdminUserResponseDto>> ToggleAdminStatusAsync(int id, bool isActive, int currentUserId);
        Task<ServiceResult<bool>> DeleteAdminAsync(int id, int currentUserId);
        Task<ServiceResult<object>> ResetAdminPasswordAsync(int id, int currentUserId);
    }
}
