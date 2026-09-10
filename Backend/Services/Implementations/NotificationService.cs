using CampusServicesPortal.DTOs.Requests.Nortifcation;
using CampusServicesPortal.DTOs.Responses.MasterData;
using CampusServicesPortal.DTOs.Responses.Notifications; // Core link to the response DTO
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusServicesPortal.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;

        public NotificationService(INotificationRepository repository)
        {
            _repository = repository;
        }

        // 🌟 UPDATED ADMIN METHOD: Transforms repository model data into the clean admin response DTO
        public async Task<IEnumerable<AdminNotificationResponseDto>> GetAllNotificationsAsync()
        {
            // Calls the new repository method that handles the native database SQL Join
            var notificationsWithStudents = await _repository.GetAllWithStudentDetailsAsync();

            // Maps the data fields so the Admin UI receives the IndexNumber string
            var response = notificationsWithStudents.Select(n => new AdminNotificationResponseDto
            {
                Id = n.Id,
                IndexNumber = n.IndexNumber, // String display mapping
                Type = n.Type,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });

            return response;
        }

        // 🛡️ STUDENT CHANNEL: Remains safely untouched and uses the standard StudentId property
        public async Task<ServiceResult<IEnumerable<NotificationResponseDto>>> GetStudentNotificationsAsync(int studentId)
        {
            var notifications = await _repository.GetByStudentIdAsync(studentId);

            var response = notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                StudentId = n.StudentId,
                Type = n.Type,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });

            return ServiceResult<IEnumerable<NotificationResponseDto>>.Success(response, 200);
        }

        public async Task<ServiceResult<object>> MarkNotificationAsReadAsync(int notificationId, int studentId)
        {
            if (studentId <= 0)
                return ServiceResult<object>.Failure("Access Denied. Only the target student recipient can mark notifications as read.", 403);

            var notification = await _repository.GetByIdAndStudentIdAsync(notificationId, studentId);

            if (notification == null)
                return ServiceResult<object>.Failure("Notification record not found or access denied.", 404);

            notification.IsRead = true;

            await _repository.UpdateAsync(notification);
            await _repository.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Notification marked as read successfully." }, 200);
        }

        public async Task<ServiceResult<object>> SendInternalNotificationAsync(CreateNotificationDto dto)
        {
            var notification = new Notification
            {
                StudentId = dto.StudentId,
                Type = dto.Type,
                Message = dto.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(notification);

            return ServiceResult<object>.Success(new { Message = "Internal system event notification staged safely." }, 201);
        }
    }
}
