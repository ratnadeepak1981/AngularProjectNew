using CampusServicesPortal.Application.Interfaces.Repositories;
using CampusServicesPortal.Data;
using CampusServicesPortal.DTOs.Requests.MasterData;
using CampusServicesPortal.DTOs.Requests.Nortifcation;
using CampusServicesPortal.DTOs.Responses.MasterData;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusServicesPortal.Services.Implementations
{
    public class FacultyService : IFacultyService
    {
        private readonly IFacultyRepository _facultyRepository;
        private readonly AppDbContext _context;

        public FacultyService(IFacultyRepository facultyRepository, AppDbContext context)
        {
            _facultyRepository = facultyRepository;
            _context = context;
        }

        #region Faculty Management Methods [PDF: 0.1.17]

        public async Task<ServiceResult<IEnumerable<FacultyResponseDto>>> GetAllFacultiesAsync()
        {
            var faculties = await _facultyRepository.GetAllAsync();
            var response = faculties.Select(f => new FacultyResponseDto
            {
                Id = f.Id,
                Name = f.Name,
                IsActive = f.IsActive
            });
            return ServiceResult<IEnumerable<FacultyResponseDto>>.Success(response, 200);
        }

        public async Task<ServiceResult<FacultyResponseDto>> CreateFacultyAsync(CreateFacultyRequestDto request)
        {
            string cleanName = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
                return ServiceResult<FacultyResponseDto>.Failure("Faculty title is required.", 400);

            if (await _facultyRepository.ExistsByNameAsync(cleanName))
                return ServiceResult<FacultyResponseDto>.Failure($"A faculty with designated title '{cleanName}' already exists.", 409);

            var faculty = new Faculty
            {
                Name = cleanName,
                IsActive = true
            };

            await _facultyRepository.AddAsync(faculty);
            await _facultyRepository.SaveChangesAsync();

            var response = new FacultyResponseDto { Id = faculty.Id, Name = faculty.Name, IsActive = faculty.IsActive };
            return ServiceResult<FacultyResponseDto>.Success(response, 201);
        }

        public async Task<ServiceResult<FacultyResponseDto>> UpdateFacultyAsync(int id, UpdateFacultyRequestDto request)
        {
            var faculty = await _facultyRepository.GetByIdAsync(id);
            if (faculty == null)
                return ServiceResult<FacultyResponseDto>.Failure("Target faculty master record not found.", 404);

            string cleanName = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
                return ServiceResult<FacultyResponseDto>.Failure("Faculty title is required.", 400);

            if (await _facultyRepository.ExistsByNameAsync(cleanName, id))
                return ServiceResult<FacultyResponseDto>.Failure($"A faculty with designated title '{cleanName}' already exists.", 409);

            faculty.Name = cleanName;
            faculty.IsActive = request.IsActive;

            await _facultyRepository.UpdateAsync(faculty);
            await _facultyRepository.SaveChangesAsync();

            var response = new FacultyResponseDto { Id = faculty.Id, Name = faculty.Name, IsActive = faculty.IsActive };
            return ServiceResult<FacultyResponseDto>.Success(response, 200);
        }

        public async Task<ServiceResult<object>> DeleteFacultyAsync(int id)
        {
            var faculty = await _facultyRepository.GetByIdAsync(id);
            if (faculty == null)
                return ServiceResult<object>.Failure("Target faculty master record not found.", 404);

            // Business Rule: A Faculty cannot be deleted while students are linked to it [PDF: 0.1.17]
            if (await _facultyRepository.HasLinkedStudentsAsync(id))
                return ServiceResult<object>.Failure("A Faculty cannot be deleted while students are linked to it — deactivate instead.", 400);

            faculty.IsActive = false; // Soft-delete mechanism [PDF: 0.1.17]
            await _facultyRepository.UpdateAsync(faculty);
            await _facultyRepository.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Faculty successfully soft-deleted or deactivated." }, 200);
        }

        #endregion

        #region Cross-Cutting System & Notification Methods

        // Rule 12: View/update the configurable reservation hold period setting [PDF: 0.1.12, 0.1.19]
        // 🆕 Rule 12: View/update the configurable reservation hold period setting [PDF: 0.1.12, 0.1.21]
        public async Task<ServiceResult<object>> UpdateGlobalSettingAsync(string key, string value)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
            if (setting == null)
            {
                setting = new SystemSetting
                {
                    SettingKey = key,
                    SettingValue = value
                };
                await _context.SystemSettings.AddAsync(setting);
            }
            else
            {
                setting.SettingValue = value;
            }

            await _context.SaveChangesAsync();
            return ServiceResult<object>.Success(new { Message = $"System setting '{key}' updated successfully." }, 200);
        }


        // Module 8: List a student's notifications, newest first [PDF: 0.1.16]
        public async Task<ServiceResult<IEnumerable<NotificationResponseDto>>> GetStudentNotificationsAsync(int studentId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.Id) // Rule: Newest first [PDF: 0.1.16]
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    StudentId = n.StudentId,
                    Type = n.Type, // E.g., HostelApproved, ComplaintUpdated [PDF: 0.1.17]
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<NotificationResponseDto>>.Success(notifications, 200);
        }

        // Module 8: Mark a notification as read safely ensuring student ownership [PDF: 0.1.16]
        public async Task<ServiceResult<object>> MarkNotificationAsReadAsync(int notificationId, int studentId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.StudentId == studentId); // Security: Own notifications only [PDF: 0.1.16]

            if (notification == null)
                return ServiceResult<object>.Failure("Notification record not found or access denied.", 404);

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Notification marked as read successfully." }, 200);
        }

        // Module 8: Server-to-server internal transaction notification creation [PDF: 0.1.17]
        public async Task<ServiceResult<object>> SendInternalNotificationAsync(CreateNotificationDto dto)
        {
            var notification = new Notification
            {
                StudentId = dto.StudentId,
                Type = dto.Type, // E.g., HostelApproved, ComplaintUpdated [PDF: 0.1.17]
                Message = dto.Message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Internal service notification logged successfully." }, 201);
        }

        #endregion
    }
}
