using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.AdminManagement;
using CampusServicesPortal.DTOs.Responses.AdminManagement;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Implementations
{
    public class AdminManagementService : IAdminManagementService
    {
        private readonly IAdminManagementRepository _adminRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IEmailService _emailService;

        public AdminManagementService(
            IAdminManagementRepository adminRepository,
            IAuditLogService auditLogService,
            IEmailService emailService)
        {
            _adminRepository = adminRepository;
            _auditLogService = auditLogService;
            _emailService = emailService;
        }

        public async Task<ServiceResult<IEnumerable<AdminUserResponseDto>>> GetAdminsAsync()
        {
            var admins = await _adminRepository.GetAdminsAsync();
            var dtos = admins.Select(MapToResponseDto).ToList();
            return ServiceResult<IEnumerable<AdminUserResponseDto>>.Success(dtos, 200);
        }

        public async Task<ServiceResult<AdminUserResponseDto>> CreateAdminAsync(CreateAdminRequestDto request, int currentUserId)
        {
            string cleanEmail = request.Email.Trim().ToLower();

            var existingUser = await _adminRepository.GetUserByEmailAsync(cleanEmail);
            if (existingUser != null)
            {
                return ServiceResult<AdminUserResponseDto>.Failure("A user profile with this email address already exists.", 409);
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newAdmin = new User
            {
                Email = cleanEmail,
                FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
                PasswordHash = passwordHash,
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastPasswordChangedAt = DateTime.UtcNow,
                MustChangePassword = false,
                FailedLoginAttempts = 0
            };

            await _adminRepository.AddUserAsync(newAdmin);
            await _adminRepository.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId: currentUserId,
                userDisplayName: cleanEmail,
                action: "AdminCreated",
                module: "AdminManagement",
                entityId: newAdmin.Id.ToString(),
                description: $"SuperAdmin created new Admin account profile for '{cleanEmail}'.",
                isSuccess: true);

            return ServiceResult<AdminUserResponseDto>.Success(MapToResponseDto(newAdmin), 201);
        }

        public async Task<ServiceResult<AdminUserResponseDto>> ToggleAdminStatusAsync(int id, bool isActive, int currentUserId)
        {
            var user = await _adminRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                return ServiceResult<AdminUserResponseDto>.Failure("Admin user account profile not found.", 404);
            }

            // Protect against deactivating the last active SuperAdmin
            if (user.Role == "SuperAdmin" && !isActive)
            {
                int activeSuperAdmins = await _adminRepository.CountActiveSuperAdminsAsync();
                if (activeSuperAdmins <= 1)
                {
                    return ServiceResult<AdminUserResponseDto>.Failure(
                        "Cannot deactivate the last active SuperAdmin account. System must always retain at least one active SuperAdmin.", 400);
                }
            }

            user.IsActive = isActive;
            _adminRepository.UpdateUser(user);
            await _adminRepository.SaveChangesAsync();

            string statusText = isActive ? "Activated" : "Deactivated";
            await _auditLogService.LogActivityAsync(
                userId: currentUserId,
                userDisplayName: user.Email,
                action: isActive ? "AdminActivated" : "AdminDeactivated",
                module: "AdminManagement",
                entityId: user.Id.ToString(),
                description: $"Admin account '{user.Email}' was {statusText.ToLower()} by SuperAdmin.",
                isSuccess: true);

            return ServiceResult<AdminUserResponseDto>.Success(MapToResponseDto(user), 200);
        }

        public async Task<ServiceResult<bool>> DeleteAdminAsync(int id, int currentUserId)
        {
            var user = await _adminRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                return ServiceResult<bool>.Failure("Admin user account profile not found.", 404);
            }

            // Prevent deleting a SuperAdmin if it's the last one
            if (user.Role == "SuperAdmin")
            {
                int activeSuperAdmins = await _adminRepository.CountActiveSuperAdminsAsync();
                if (activeSuperAdmins <= 1)
                {
                    return ServiceResult<bool>.Failure(
                        "Cannot delete the last active SuperAdmin account. System must always retain at least one active SuperAdmin.", 400);
                }
            }

            string userEmail = user.Email;
            _adminRepository.DeleteUser(user);
            await _adminRepository.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId: currentUserId,
                userDisplayName: userEmail,
                action: "AdminDeleted",
                module: "AdminManagement",
                entityId: id.ToString(),
                description: $"Admin account '{userEmail}' was deleted by SuperAdmin.",
                isSuccess: true);

            return ServiceResult<bool>.Success(true, 200);
        }

        public async Task<ServiceResult<object>> ResetAdminPasswordAsync(int id, int currentUserId)
        {
            var user = await _adminRepository.GetUserByIdAsync(id);
            if (user == null)
            {
                return ServiceResult<object>.Failure("Admin user account profile not found.", 404);
            }

            if (user.Role == "SuperAdmin")
            {
                return ServiceResult<object>.Failure("Password reset for SuperAdmin accounts cannot be performed through Admin User Management.", 400);
            }

            user.MustChangePassword = true;
            user.TemporaryPasswordExpiresAt = DateTime.UtcNow.AddHours(24);
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;

            _adminRepository.UpdateUser(user);
            await _adminRepository.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(user.Email) && _emailService != null)
            {
                try
                {
                    var emailPreview = await _emailService.GeneratePasswordResetEmailPreviewAsync(user.Email);
                    if (emailPreview.IsSuccess && !string.IsNullOrWhiteSpace(emailPreview.Data))
                    {
                        await _emailService.SendEmailAsync(user.Email, "Administrator Password Reset - Campus Services Portal", emailPreview.Data);
                    }
                }
                catch
                {
                    // Graceful email handling
                }
            }

            await _auditLogService.LogActivityAsync(
                userId: currentUserId,
                userDisplayName: user.Email,
                action: "AdminPasswordResetInitiated",
                module: "AdminManagement",
                entityId: user.Id.ToString(),
                description: $"SuperAdmin initiated password reset and account unlock for Admin '{user.Email}'.",
                isSuccess: true);

            return ServiceResult<object>.Success(new
            {
                Message = $"Password reset initiated and account unlocked for Admin user '{user.Email}'.",
                Email = user.Email
            }, 200);
        }

        private static AdminUserResponseDto MapToResponseDto(User user)
        {
            return new AdminUserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastPasswordChangedAt = user.LastPasswordChangedAt
            };
        }
    }
}
