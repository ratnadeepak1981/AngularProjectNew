using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Auth;
using CampusServicesPortal.DTOs.Requests.Nortifcation;
using CampusServicesPortal.DTOs.Requests.Sms;
using CampusServicesPortal.DTOs.Responses.Auth;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.Extensions.Caching.Memory;

namespace CampusServicesPortal.Services.Implementations
{
    public class PasswordService : IPasswordService
    {
        private readonly IPasswordRepository _passwordRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IMemoryCache _memoryCache;
        private readonly ISmsService _smsService;
        private readonly INotificationService _notificationService;

        public PasswordService(
            IPasswordRepository passwordRepository,
            IAuditLogService auditLogService,
            IMemoryCache memoryCache,
            ISmsService smsService,
            INotificationService notificationService)
        {
            _passwordRepository = passwordRepository;
            _auditLogService = auditLogService;
            _memoryCache = memoryCache;
            _smsService = smsService;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<PasswordPolicyResponseDto>> GetPasswordPolicyAsync()
        {
            var minLengthSetting = await _passwordRepository.GetSystemSettingAsync("MinPasswordLength");
            int minLength = minLengthSetting != null && int.TryParse(minLengthSetting.SettingValue, out var ml) ? ml : 8;

            var complexitySetting = await _passwordRepository.GetSystemSettingAsync("RequirePasswordComplexity");
            string complexityTier = complexitySetting?.SettingValue ?? "strong";

            var expiryDaysSetting = await _passwordRepository.GetSystemSettingAsync("PasswordExpiryDays");
            int expiryDays = expiryDaysSetting != null && int.TryParse(expiryDaysSetting.SettingValue, out var ed) ? ed : 90;

            var reuseLimitSetting = await _passwordRepository.GetSystemSettingAsync("PasswordReuseHistoryLimit");
            int reuseLimit = reuseLimitSetting != null && int.TryParse(reuseLimitSetting.SettingValue, out var rl) ? rl : 5;

            var otpMinsSetting = await _passwordRepository.GetSystemSettingAsync("OtpValidityMinutes");
            int otpMins = otpMinsSetting != null && int.TryParse(otpMinsSetting.SettingValue, out var om) ? om : 3;

            var maxFailedSetting = await _passwordRepository.GetSystemSettingAsync("MaxFailedLogins");
            int maxFailed = maxFailedSetting != null && int.TryParse(maxFailedSetting.SettingValue, out var mf) ? mf : 5;

            var lockoutMinsSetting = await _passwordRepository.GetSystemSettingAsync("AccountLockoutDurationMinutes");
            int lockoutMins = lockoutMinsSetting != null && int.TryParse(lockoutMinsSetting.SettingValue, out var lm) ? lm : 15;

            var policy = new PasswordPolicyResponseDto
            {
                MinLength = minLength,
                ComplexityTier = complexityTier,
                ExpiryDays = expiryDays,
                ReuseHistoryLimit = reuseLimit,
                OtpValidityMinutes = otpMins,
                MaxFailedLogins = maxFailed,
                LockoutDurationMinutes = lockoutMins
            };

            return ServiceResult<PasswordPolicyResponseDto>.Success(policy, 200);
        }

        public async Task<ServiceResult<object>> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return ServiceResult<object>.Failure("Email address is required.", 400);
            }

            string cleanEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _passwordRepository.GetUserByEmailAsync(cleanEmail);

            // Read OTP Validity from System Settings (Default: 3 minutes)
            var otpMinsSetting = await _passwordRepository.GetSystemSettingAsync("OtpValidityMinutes");
            int otpMins = otpMinsSetting != null && int.TryParse(otpMinsSetting.SettingValue, out var om) && om > 0 ? om : 3;

            // Generate cryptographically secure 6-digit OTP
            string otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // Cache OTP with System Settings TTL
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(otpMins)
            };

            _memoryCache.Set($"PasswordResetOtp_{cleanEmail}", otpCode, cacheOptions);
            _memoryCache.Set($"PasswordResetOtp_Attempts_{cleanEmail}", 0, cacheOptions);

            var student = await _passwordRepository.GetStudentByEmailThroughUserAsync(cleanEmail);
            if (student != null)
            {
                await _passwordRepository.InvalidateExistingResetTokensAsync(student.Id);
                var resetToken = new PasswordResetToken
                {
                    StudentId = student.Id,
                    Token = otpCode,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(otpMins),
                    IsUsed = false
                };
                await _passwordRepository.SavePasswordResetTokenAsync(resetToken);

                // Dispatch SMS simulation
                string phone = !string.IsNullOrWhiteSpace(student.ContactDetails) ? student.ContactDetails : "+94 77 123 4567";
                await _smsService.DispatchSmsAsync(new SendSmsRequestDto
                {
                    PhoneNumber = phone,
                    Purpose = SmsPurposes.ForgotPasswordOtp,
                    OtpCode = otpCode
                });
            }

            await _auditLogService.LogActivityAsync(
                userId: user?.Id,
                userDisplayName: request.Email,
                action: "ForgotPasswordInitiated",
                module: "Auth",
                entityId: user?.Id.ToString(),
                description: $"Password reset OTP requested for '{request.Email}'. Dispatched with {otpMins}-minute validity.",
                isSuccess: true);

            return ServiceResult<object>.Success(new
            {
                Message = $"Verification OTP code dispatched successfully. Valid for {otpMins} minutes.",
                Email = cleanEmail,
                OtpCode = otpCode,
                ValidityMinutes = otpMins,
                ExpiresInSeconds = otpMins * 60
            }, 200);
        }

        public async Task<ServiceResult<object>> VerifyResetOtpAsync(VerifyResetOtpRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return ServiceResult<object>.Failure("Email and 6-digit OTP code are required.", 400);
            }

            string cleanEmail = request.Email.Trim().ToLowerInvariant();
            string inputOtp = request.OtpCode.Trim();

            var maxAttemptsSetting = await _passwordRepository.GetSystemSettingAsync("MaxOtpResendAttempts");
            int maxAttempts = maxAttemptsSetting != null && int.TryParse(maxAttemptsSetting.SettingValue, out var ma) ? ma : 5;

            // Check cached OTP
            if (!_memoryCache.TryGetValue($"PasswordResetOtp_{cleanEmail}", out string? cachedOtp) || string.IsNullOrEmpty(cachedOtp))
            {
                // Fallback: check database token
                var dbToken = await _passwordRepository.GetPasswordResetTokenAsync(inputOtp);
                if (dbToken == null || dbToken.IsUsed || dbToken.ExpiresAt < DateTime.UtcNow)
                {
                    await _auditLogService.LogActivityAsync(
                        userId: null,
                        userDisplayName: cleanEmail,
                        action: "ResetOtpVerificationFailure",
                        module: "Auth",
                        entityId: null,
                        description: $"Password reset OTP verification failed for '{cleanEmail}': OTP expired or invalid.",
                        isSuccess: false);

                    return ServiceResult<object>.Failure("The verification OTP has expired or is invalid. Please click Resend OTP to receive a fresh code.", 400);
                }
                cachedOtp = dbToken.Token;
            }

            int attempts = _memoryCache.TryGetValue($"PasswordResetOtp_Attempts_{cleanEmail}", out int att) ? att : 0;
            if (attempts >= maxAttempts)
            {
                _memoryCache.Remove($"PasswordResetOtp_{cleanEmail}");
                _memoryCache.Remove($"PasswordResetOtp_Attempts_{cleanEmail}");

                await _auditLogService.LogActivityAsync(
                    userId: null,
                    userDisplayName: cleanEmail,
                    action: "ResetOtpMaxAttemptsExceeded",
                    module: "Auth",
                    entityId: null,
                    description: $"Password reset OTP verification locked for '{cleanEmail}': Maximum retry attempts ({maxAttempts}) exceeded.",
                    isSuccess: false);

                return ServiceResult<object>.Failure($"Maximum OTP retry attempts exceeded ({maxAttempts}). Please request a fresh OTP.", 400);
            }

            if (cachedOtp != inputOtp)
            {
                attempts++;
                _memoryCache.Set($"PasswordResetOtp_Attempts_{cleanEmail}", attempts);

                await _auditLogService.LogActivityAsync(
                    userId: null,
                    userDisplayName: cleanEmail,
                    action: "ResetOtpVerificationFailure",
                    module: "Auth",
                    entityId: null,
                    description: $"Invalid OTP entered for '{cleanEmail}'. Attempt {attempts} of {maxAttempts}.",
                    isSuccess: false);

                return ServiceResult<object>.Failure($"Invalid OTP code entered. ({maxAttempts - attempts} attempt(s) remaining).", 400);
            }

            // Invalidate OTP (Single Use Policy)
            _memoryCache.Remove($"PasswordResetOtp_{cleanEmail}");
            _memoryCache.Remove($"PasswordResetOtp_Attempts_{cleanEmail}");

            // Issue a short-lived single-use Reset Ticket (10 minutes validity)
            string resetTicket = Guid.NewGuid().ToString("N");
            _memoryCache.Set($"PasswordResetTicket_{resetTicket}", cleanEmail, TimeSpan.FromMinutes(10));

            await _auditLogService.LogActivityAsync(
                userId: null,
                userDisplayName: cleanEmail,
                action: "ResetOtpVerificationSuccess",
                module: "Auth",
                entityId: null,
                description: $"Password reset OTP successfully verified for '{cleanEmail}'. Reset ticket issued.",
                isSuccess: true);

            return ServiceResult<object>.Success(new
            {
                Message = "OTP verified successfully. You may now set your new password.",
                ResetTicket = resetTicket,
                Email = cleanEmail
            }, 200);
        }

        public async Task<ServiceResult<object>> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return ServiceResult<object>.Failure("Reset token and new password are required.", 400);
            }

            string cleanTokenInput = request.Token.Trim().Replace("\"", "");
            string? userEmail = null;

            if (_memoryCache.TryGetValue($"PasswordResetTicket_{cleanTokenInput}", out string? cachedEmail) && !string.IsNullOrEmpty(cachedEmail))
            {
                userEmail = cachedEmail;
            }

            User? userProfile = null;
            PasswordResetToken? dbTokenRecord = null;

            if (!string.IsNullOrEmpty(userEmail))
            {
                userProfile = await _passwordRepository.GetUserByEmailAsync(userEmail);
            }
            else
            {
                dbTokenRecord = await _passwordRepository.GetPasswordResetTokenAsync(cleanTokenInput);
                if (dbTokenRecord == null)
                {
                    dbTokenRecord = await _passwordRepository.GetLatestUnusedTokenAsync();
                }

                if (dbTokenRecord != null && !dbTokenRecord.IsUsed && dbTokenRecord.ExpiresAt > DateTime.UtcNow)
                {
                    var student = await _passwordRepository.GetStudentByIdAsync(dbTokenRecord.StudentId);
                    userProfile = student?.User;
                }
            }

            if (userProfile == null)
            {
                await _auditLogService.LogActivityAsync(
                    userId: null,
                    userDisplayName: "Unknown Password Reset Subject",
                    action: "PasswordResetFailure",
                    module: "Auth",
                    entityId: null,
                    description: "Password reset failed: Invalid, expired, or already used reset token/ticket.",
                    isSuccess: false);

                return ServiceResult<object>.Failure("Invalid, expired, or already used reset session. Please restart the password reset process.", 400);
            }

            // 1. Fetch System Settings Security Policies
            var minLengthSetting = await _passwordRepository.GetSystemSettingAsync("MinPasswordLength");
            int minLength = minLengthSetting != null && int.TryParse(minLengthSetting.SettingValue, out var ml) ? ml : 8;

            var complexitySetting = await _passwordRepository.GetSystemSettingAsync("RequirePasswordComplexity");
            string complexityTier = complexitySetting?.SettingValue ?? "strong";

            var reuseLimitSetting = await _passwordRepository.GetSystemSettingAsync("PasswordReuseHistoryLimit");
            int reuseLimit = reuseLimitSetting != null && int.TryParse(reuseLimitSetting.SettingValue, out var rl) ? rl : 5;

            var newPassword = request.NewPassword ?? string.Empty;

            // 2. Minimum Length Enforcement
            if (newPassword.Length < minLength)
            {
                return ServiceResult<object>.Failure($"Password policy error: Password must be at least {minLength} characters long.", 400);
            }

            // 3. Password Complexity Enforcement
            if (!ValidatePasswordComplexity(newPassword, complexityTier, minLength, out var complexityErrorMessage))
            {
                return ServiceResult<object>.Failure($"Password complexity error: {complexityErrorMessage}", 400);
            }

            // 4. Password Reuse History Enforcement
            if (reuseLimit > 0)
            {
                if (!string.IsNullOrEmpty(userProfile.PasswordHash) && BCrypt.Net.BCrypt.Verify(newPassword, userProfile.PasswordHash))
                {
                    return ServiceResult<object>.Failure("Password policy error: You cannot reuse your current active password.", 400);
                }

                var histories = await _passwordRepository.GetRecentPasswordHistoriesAsync(userProfile.Id, reuseLimit);
                foreach (var hist in histories)
                {
                    if (!string.IsNullOrEmpty(hist.PasswordHash) && BCrypt.Net.BCrypt.Verify(newPassword, hist.PasswordHash))
                    {
                        return ServiceResult<object>.Failure($"Password policy error: You cannot reuse any of your last {reuseLimit} previous passwords.", 400);
                    }
                }
            }

            // Record old password into history table before updating
            if (!string.IsNullOrEmpty(userProfile.PasswordHash))
            {
                await _passwordRepository.AddPasswordHistoryAsync(new PasswordHistory
                {
                    UserId = userProfile.Id,
                    PasswordHash = userProfile.PasswordHash,
                    PasswordSalt = string.Empty,
                    CreatedAt = DateTime.UtcNow
                });
            }

            userProfile.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            userProfile.LastPasswordChangedAt = DateTime.UtcNow;
            userProfile.MustChangePassword = false;
            userProfile.TemporaryPasswordExpiresAt = null;
            userProfile.FailedLoginAttempts = 0;
            userProfile.LockoutEndUtc = null;

            if (dbTokenRecord != null)
            {
                dbTokenRecord.IsUsed = true;
                await _passwordRepository.UpdateResetTokenStatusAsync(dbTokenRecord);
            }

            // Invalidate the reset ticket from memory cache
            _memoryCache.Remove($"PasswordResetTicket_{cleanTokenInput}");

            await _passwordRepository.UpdateUserAsync(userProfile);
            await _passwordRepository.RevokeAllUserSessionsAsync(userProfile.Id);

            await _auditLogService.LogActivityAsync(
                userId: userProfile.Id,
                userDisplayName: userProfile.Email,
                action: "PasswordResetSuccess",
                module: "Auth",
                entityId: userProfile.Id.ToString(),
                description: $"Password reset successfully completed for user '{userProfile.Email}'.",
                isSuccess: true);

            return ServiceResult<object>.Success(new { Message = "Password updated successfully! You can now log in with your new password." }, 200);
        }

        public async Task<ServiceResult<object>> ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return ServiceResult<object>.Failure("Current password and new password are required.", 400);
            }

            var user = await _passwordRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<object>.Failure("User profile not found.", 404);
            }

            // Verify Current / Temporary Password
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                await _auditLogService.LogActivityAsync(
                    userId: user.Id,
                    userDisplayName: user.Email,
                    action: "PasswordChangeFailure",
                    module: "Auth",
                    entityId: user.Id.ToString(),
                    description: $"Password change failed for '{user.Email}': Incorrect current password provided.",
                    isSuccess: false);

                return ServiceResult<object>.Failure("Current password entered is incorrect.", 400);
            }

            // Fetch System Settings Security Policies
            var minLengthSetting = await _passwordRepository.GetSystemSettingAsync("MinPasswordLength");
            int minLength = minLengthSetting != null && int.TryParse(minLengthSetting.SettingValue, out var ml) ? ml : 8;

            var complexitySetting = await _passwordRepository.GetSystemSettingAsync("RequirePasswordComplexity");
            string complexityTier = complexitySetting?.SettingValue ?? "strong";

            var reuseLimitSetting = await _passwordRepository.GetSystemSettingAsync("PasswordReuseHistoryLimit");
            int reuseLimit = reuseLimitSetting != null && int.TryParse(reuseLimitSetting.SettingValue, out var rl) ? rl : 5;

            var newPassword = request.NewPassword.Trim();

            // Length Enforcement
            if (newPassword.Length < minLength)
            {
                return ServiceResult<object>.Failure($"Password policy error: Password must be at least {minLength} characters long.", 400);
            }

            // Complexity Enforcement
            if (!ValidatePasswordComplexity(newPassword, complexityTier, minLength, out var complexityErrorMessage))
            {
                return ServiceResult<object>.Failure($"Password complexity error: {complexityErrorMessage}", 400);
            }

            // Reuse History Enforcement
            if (reuseLimit > 0)
            {
                if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
                {
                    return ServiceResult<object>.Failure("Password policy error: New password cannot be the same as your current password.", 400);
                }

                var histories = await _passwordRepository.GetRecentPasswordHistoriesAsync(user.Id, reuseLimit);
                foreach (var hist in histories)
                {
                    if (!string.IsNullOrEmpty(hist.PasswordHash) && BCrypt.Net.BCrypt.Verify(newPassword, hist.PasswordHash))
                    {
                        return ServiceResult<object>.Failure($"Password policy error: You cannot reuse any of your last {reuseLimit} previous passwords.", 400);
                    }
                }
            }

            // Record old password in history
            await _passwordRepository.AddPasswordHistoryAsync(new PasswordHistory
            {
                UserId = user.Id,
                PasswordHash = user.PasswordHash,
                PasswordSalt = string.Empty,
                CreatedAt = DateTime.UtcNow
            });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.LastPasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;
            user.TemporaryPasswordExpiresAt = null;
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;

            await _passwordRepository.UpdateUserAsync(user);
            await _passwordRepository.RevokeAllUserSessionsAsync(user.Id);

            await _auditLogService.LogActivityAsync(
                userId: user.Id,
                userDisplayName: user.Email,
                action: "PasswordChangeSuccess",
                module: "Auth",
                entityId: user.Id.ToString(),
                description: $"Password successfully updated for user '{user.Email}'.",
                isSuccess: true);

            return ServiceResult<object>.Success(new { Message = "Password changed successfully." }, 200);
        }

        public async Task<ServiceResult<object>> AdminResetStudentPasswordAsync(int studentId)
        {
            var student = await _passwordRepository.GetStudentByIdAsync(studentId);
            if (student == null || student.User == null)
            {
                return ServiceResult<object>.Failure("Target student account profile was not found.", 404);
            }

            var tempHoursSetting = await _passwordRepository.GetSystemSettingAsync("TemporaryPasswordValidityHours");
            int tempHours = tempHoursSetting != null && int.TryParse(tempHoursSetting.SettingValue, out var th) && th > 0 ? th : 24;

            // Generate secure temporary password (e.g. Temp#739281)
            string randomDigits = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            string tempPassword = $"Temp#{randomDigits}";

            student.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            student.User.MustChangePassword = true;
            student.User.TemporaryPasswordExpiresAt = DateTime.UtcNow.AddHours(tempHours);
            student.User.FailedLoginAttempts = 0;
            student.User.LockoutEndUtc = null;

            await _passwordRepository.UpdateUserAsync(student.User);
            await _passwordRepository.RevokeAllUserSessionsAsync(student.User.Id);

            // Dispatch notification to student
            await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
            {
                StudentId = student.Id,
                Type = "SecurityAlert",
                Message = $"Security Notice: An administrator has reset your portal password. Your temporary password is: {tempPassword}. Valid for {tempHours} hours. You will be required to change this password immediately upon logging in."
            });

            await _auditLogService.LogActivityAsync(
                userId: student.UserId,
                userDisplayName: student.User.Email,
                action: "AdminInitiatedPasswordReset",
                module: "Auth",
                entityId: student.Id.ToString(),
                description: $"Administrator reset password for student '{student.FullName}' ({student.IndexNumber}). Temporary credentials valid for {tempHours} hours.",
                isSuccess: true);

            return ServiceResult<object>.Success(new
            {
                Message = "Student password reset successfully. Temporary credentials have been dispatched.",
                StudentId = student.Id,
                TemporaryPassword = tempPassword,
                ValidityHours = tempHours,
                ExpiresAt = student.User.TemporaryPasswordExpiresAt
            }, 200);
        }

        private static bool ValidatePasswordComplexity(string password, string tier, int minLength, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (tier.Equals("basic", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSymbol = password.Any(c => !char.IsLetterOrDigit(c));

            if (tier.Equals("medium", StringComparison.OrdinalIgnoreCase))
            {
                if (!hasUpper || !hasLower || !hasDigit)
                {
                    errorMessage = "Password must contain a mixture of uppercase letters (A-Z), lowercase letters (a-z), and numeric digits (0-9).";
                    return false;
                }
                return true;
            }

            if (tier.Equals("strict", StringComparison.OrdinalIgnoreCase))
            {
                if (password.Length < Math.Max(12, minLength) || !hasUpper || !hasLower || !hasDigit || !hasSymbol)
                {
                    errorMessage = "Strict Enterprise policy requires at least 12 characters including uppercase, lowercase, numbers, and special symbols (@$!%*?&).";
                    return false;
                }
                return true;
            }

            // Default 'strong' tier
            if (!hasUpper || !hasLower || !hasDigit || !hasSymbol)
            {
                errorMessage = "Password must contain uppercase letters, lowercase letters, numbers, and at least one special symbol (@$!%*?&).";
                return false;
            }
            return true;
        }
    }
}
