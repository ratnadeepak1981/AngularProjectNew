using CampusServicesPortal.DTOs.Requests.Nortifcation;
using CampusServicesPortal.DTOs.Responses.MasterData;
using CampusServicesPortal.DTOs.Responses.Notifications;
using CampusServicesPortal.Models;
using CampusServicesPortal.Wrappers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusServicesPortal.Services.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<AdminNotificationResponseDto>> GetAllNotificationsAsync();
        // Student Channel: Fetch logs arranged in reverse chronological sequence [PDF: 0.1.16]
        Task<ServiceResult<IEnumerable<NotificationResponseDto>>> GetStudentNotificationsAsync(int studentId);

        // Student Channel: Clear attention alerts and flip status bits [PDF: 0.1.16]
        Task<ServiceResult<object>> MarkNotificationAsReadAsync(int notificationId, int studentId);

        // System Channel: Handle automated event hook alerts across modules [PDF: 0.1.17]
        Task<ServiceResult<object>> SendInternalNotificationAsync(CreateNotificationDto dto);
    }
}
