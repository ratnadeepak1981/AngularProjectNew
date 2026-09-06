using CampusServicesPortal.DTOs.Requests.Nortifcation;
using CampusServicesPortal.DTOs.Responses.MasterData;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories;
using CampusServicesPortal.Repositories.Interfaces; // Fixed namespace mapping parameter references
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

        public async Task<IEnumerable<Notification>> GetAllNotificationsAsync()
        {
            // Calls the new repository tracking data query method we created
            return await _repository.GetAllAsync();
        }

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

            // Stage modification parameters inside EF tracking memory pool context
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

            // 1. Stage the notification insertion into the shared AppDbContext cache pool [INDEX]
            await _repository.AddAsync(notification);

            // 2. FIXED: REMOVED the redundant intermediate SaveChangesAsync() call [INDEX]
            // This leaves the parent service orchestrator (e.g., HostelService) to commit everything 
            // together in a single transaction round-trip! [INDEX]

            return ServiceResult<object>.Success(new { Message = "Internal system event notification staged safely." }, 201);
        }
    }
}
