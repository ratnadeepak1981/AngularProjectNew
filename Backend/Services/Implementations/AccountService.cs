using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CampusServicesPortal.Data;
using CampusServicesPortal.DTOs.Requests.Auth;
using CampusServicesPortal.DTOs.Requests.Sms;
using CampusServicesPortal.DTOs.Responses.Auth;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CampusServicesPortal.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ISmsService _smsService;
        private readonly IMemoryCache _memoryCache;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public AccountService(
            IAccountRepository accountRepository,
            IStudentRepository studentRepository,
            ISmsService smsService,
            IMemoryCache memoryCache,
            AppDbContext context,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _accountRepository = accountRepository;
            _studentRepository = studentRepository;
            _smsService = smsService;
            _memoryCache = memoryCache;
            _context = context;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        private static string NormalizePhoneKey(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : Regex.Replace(raw, @"[^\d]", "");
        }

        private async Task<int> GetOtpValidityMinutesAsync()
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "OtpValidityMinutes");
                if (setting != null && int.TryParse(setting.SettingValue, out int mins) && mins > 0)
                {
                    return mins;
                }
            }
            catch
            {
                // Fallback to 3 minutes
            }
            return 3;
        }

        public async Task<ServiceResult<bool>> VerifyEmailAsync(VerifyEmailRequestDto request)
        {
            var student = await _accountRepository.GetStudentByVerificationTokenAsync(request.Token);
            if (student == null)
            {
                await _auditLogService.LogActivityAsync(
                    userId: null,
                    userDisplayName: "Unknown Verification Subject",
                    action: "EmailVerificationFailure",
                    module: "Auth",
                    entityId: null,
                    description: "Email verification failed: Invalid or non-existent verification token.",
                    isSuccess: false);

                return ServiceResult<bool>.Failure("Invalid or expired email verification token.", 400);
            }

            DateTime emailExpiresAtUtc = DateTime.SpecifyKind(student.EmailVerificationTokenExpiresAt ?? DateTime.MinValue, DateTimeKind.Utc);
            if (emailExpiresAtUtc < DateTime.UtcNow)
            {
                await _auditLogService.LogActivityAsync(
                    userId: student.UserId,
                    userDisplayName: student.User?.Email ?? student.IndexNumber,
                    action: "EmailVerificationFailure",
                    module: "Auth",
                    entityId: student.Id.ToString(),
                    description: $"Email verification failed for student '{student.IndexNumber}': Verification token expired.",
                    isSuccess: false);

                return ServiceResult<bool>.Failure("Invalid or expired email verification token.", 400);
            }

            student.EmailVerified = true;
            student.EmailVerificationToken = null;
            student.EmailVerificationTokenExpiresAt = null;

            await _accountRepository.UpdateStudentAsync(student);
            return ServiceResult<bool>.Success(true, 200);
        }

        public async Task<ServiceResult<bool>> ResendVerificationEmailAsync(ResendVerificationRequestDto request)
        {
            var student = await _accountRepository.GetStudentByEmailThroughUserAsync(request.Email);

            if (student == null)
                return ServiceResult<bool>.Success(true, 200);

            if (student.EmailVerified)
                return ServiceResult<bool>.Failure("This account has already been verified and is active.", 400);

            student.EmailVerificationToken = Guid.NewGuid().ToString("N");
            student.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);

            await _accountRepository.UpdateStudentAsync(student);

            return ServiceResult<bool>.Success(true, 200);
        }

        public async Task<ServiceResult<object>> SendPhoneOtpAsync(SendPhoneOtpRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return ServiceResult<object>.Failure("Phone number is required to dispatch OTP.", 400);
            }

            var purpose = request.Purpose.Equals("ProfileUpdate", StringComparison.OrdinalIgnoreCase)
                ? SmsPurposes.PrimaryMobileUpdateOtp
                : SmsPurposes.RegistrationOtp;

            // Dynamically query Admin-Configurable validity (Default: 3 minutes)
            int validityMinutes = await GetOtpValidityMinutesAsync();

            // Cryptographically secure random 6-digit OTP code (e.g. 100000 - 999999)
            string otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // Cache with dynamic System Settings expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(validityMinutes)
            };

            string cleanPhone = NormalizePhoneKey(request.PhoneNumber);
            if (!string.IsNullOrEmpty(cleanPhone))
            {
                _memoryCache.Set($"PhoneOtp_Phone_{cleanPhone}", otpCode, cacheOptions);
            }

            if (!string.IsNullOrWhiteSpace(request.EmailOrIndex))
            {
                _memoryCache.Set($"PhoneOtp_User_{request.EmailOrIndex.Trim().ToLowerInvariant()}", otpCode, cacheOptions);
            }

            await _smsService.DispatchSmsAsync(new SendSmsRequestDto
            {
                PhoneNumber = request.PhoneNumber,
                Purpose = purpose,
                OtpCode = otpCode
            });

            return ServiceResult<object>.Success(new
            {
                Message = $"SMS verification OTP code dispatched to {request.PhoneNumber}. Valid for {validityMinutes} minutes.",
                PhoneNumber = request.PhoneNumber,
                ValidityMinutes = validityMinutes,
                ExpiresInSeconds = validityMinutes * 60
            }, 200);
        }

        public async Task<ServiceResult<bool>> VerifyPhoneOtpAsync(VerifyPhoneOtpRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return ServiceResult<bool>.Failure("OTP code is required.", 400);
            }

            int validityMinutes = await GetOtpValidityMinutesAsync();
            string inputOtp = request.OtpCode.Trim();
            string cleanPhone = NormalizePhoneKey(request.PhoneNumber);
            string userKey = request.EmailOrIndex?.Trim().ToLowerInvariant() ?? string.Empty;

            // Check cached OTP
            bool foundInCache = false;
            string? cachedOtp = null;

            if (!string.IsNullOrEmpty(cleanPhone) && _memoryCache.TryGetValue($"PhoneOtp_Phone_{cleanPhone}", out string? phoneOtp))
            {
                foundInCache = true;
                cachedOtp = phoneOtp;
            }
            else if (!string.IsNullOrEmpty(userKey) && _memoryCache.TryGetValue($"PhoneOtp_User_{userKey}", out string? userOtp))
            {
                foundInCache = true;
                cachedOtp = userOtp;
            }

            if (!foundInCache)
            {
                await _auditLogService.LogActivityAsync(
                    userId: null,
                    userDisplayName: !string.IsNullOrWhiteSpace(request.EmailOrIndex) ? request.EmailOrIndex : request.PhoneNumber,
                    action: "OtpFailure",
                    module: "Auth",
                    entityId: null,
                    description: $"SMS OTP verification failed for {request.PhoneNumber}: Verification OTP has expired ({validityMinutes}-minute limit).",
                    isSuccess: false);

                if (!string.IsNullOrWhiteSpace(request.EmailOrIndex))
                {
                    var st = await _studentRepository.GetByIndexOrEmailAsync(request.EmailOrIndex);
                    if (st != null)
                    {
                        await _notificationService.SendInternalNotificationAsync(new DTOs.Requests.Nortifcation.CreateNotificationDto
                        {
                            StudentId = st.Id,
                            Type = "SecurityAlert",
                            Message = $"Security Alert: SMS OTP verification expired for student {st.IndexNumber} ({request.PhoneNumber})."
                        });
                    }
                }

                return ServiceResult<bool>.Failure($"The verification OTP has expired ({validityMinutes}-minute limit). Please click Resend SMS OTP to receive a fresh code.", 400);
            }

            if (cachedOtp != inputOtp)
            {
                await _auditLogService.LogActivityAsync(
                    userId: null,
                    userDisplayName: !string.IsNullOrWhiteSpace(request.EmailOrIndex) ? request.EmailOrIndex : request.PhoneNumber,
                    action: "OtpFailure",
                    module: "Auth",
                    entityId: null,
                    description: $"SMS OTP verification failed for {request.PhoneNumber}: Invalid OTP code entered.",
                    isSuccess: false);

                if (!string.IsNullOrWhiteSpace(request.EmailOrIndex))
                {
                    var st = await _studentRepository.GetByIndexOrEmailAsync(request.EmailOrIndex);
                    if (st != null)
                    {
                        await _notificationService.SendInternalNotificationAsync(new DTOs.Requests.Nortifcation.CreateNotificationDto
                        {
                            StudentId = st.Id,
                            Type = "SecurityAlert",
                            Message = $"Security Alert: Invalid SMS OTP entered for student {st.IndexNumber} ({request.PhoneNumber})."
                        });
                    }
                }

                return ServiceResult<bool>.Failure("Invalid OTP code entered. Please enter the valid 6-digit verification code.", 400);
            }

            // Immediately invalidate/remove used OTP from cache (Single-Use Policy)
            if (!string.IsNullOrEmpty(cleanPhone))
            {
                _memoryCache.Remove($"PhoneOtp_Phone_{cleanPhone}");
            }
            if (!string.IsNullOrEmpty(userKey))
            {
                _memoryCache.Remove($"PhoneOtp_User_{userKey}");
            }

            CampusServicesPortal.Models.Student? student = null;
            if (!string.IsNullOrWhiteSpace(request.EmailOrIndex))
            {
                student = await _studentRepository.GetByIndexOrEmailAsync(request.EmailOrIndex);
            }
            else if (!string.IsNullOrEmpty(cleanPhone))
            {
                var allStudents = await _studentRepository.SearchStudentsAsync(null, null);
                student = allStudents.FirstOrDefault(s => s.PhoneNumbers.Any(p => NormalizePhoneKey(p.PhoneNumber) == cleanPhone) 
                    || NormalizePhoneKey(s.ContactDetails ?? string.Empty) == cleanPhone);
            }

            if (student != null)
            {
                var targetPhone = student.PhoneNumbers.FirstOrDefault(p => NormalizePhoneKey(p.PhoneNumber) == cleanPhone)
                                   ?? student.PhoneNumbers.FirstOrDefault(p => p.IsPrimary) 
                                   ?? student.PhoneNumbers.FirstOrDefault();

                if (targetPhone != null)
                {
                    targetPhone.IsVerified = true;
                    var prim = student.PhoneNumbers.FirstOrDefault(p => p.IsPrimary) ?? targetPhone;
                    student.ContactDetails = prim.PhoneNumber.Trim();
                    await _studentRepository.UpdateAsync(student);
                    await _studentRepository.SaveChangesAsync();
                }
            }

            return ServiceResult<bool>.Success(true, 200);
        }

        public async Task<ServiceResult<DeactivationCheckResponseDto>> CheckDeactivationEligibilityAsync(int studentId)
        {
            var reasons = new List<string>();

            if (await _accountRepository.HasActiveHostelRoomAsync(studentId))
                reasons.Add("Student currently holds active hostel room assignments.");

            if (await _accountRepository.HasUpcomingLabBookingsAsync(studentId))
                reasons.Add("Student has upcoming future-dated laboratory bookings.");

            if (await _accountRepository.HasUpcomingEventRegistrationsAsync(studentId))
                reasons.Add("Student is registered for upcoming university events.");

            if (await _accountRepository.HasPendingCertificateRequestsAsync(studentId))
                reasons.Add("Student has an outstanding certificate issuance request processing.");

            if (await _accountRepository.HasOutstandingFeesAsync(studentId))
                reasons.Add("Student has outstanding unpaid fee balances or pending fines.");

            var response = new DeactivationCheckResponseDto
            {
                CanDeactivate = reasons.Count == 0,
                BlockingReasons = reasons
            };

            return ServiceResult<DeactivationCheckResponseDto>.Success(response, 200);
        }

        public async Task<ServiceResult<object>> DeactivateAccountAsync(int studentId)
        {
            var student = await _accountRepository.GetStudentByIdAsync(studentId);
            if (student == null)
                return ServiceResult<object>.Failure("Target student account profile record was not found.", 404);

            var eligibility = await CheckDeactivationEligibilityAsync(studentId);
            if (!eligibility.Data.CanDeactivate)
            {
                return ServiceResult<object>.Failure($"Deactivation Blocked: {string.Join(" ", eligibility.Data.BlockingReasons)}", 400);
            }

            await _accountRepository.DeactivateStudentAccountAsync(studentId);
            return ServiceResult<object>.Success(new { Message = "Student portal account deactivated safely." }, 200);
        }

        public async Task<ServiceResult<object>> ReactivateAccountAsync(int studentId)
        {
            var student = await _accountRepository.GetStudentByIdAsync(studentId);
            if (student == null)
                return ServiceResult<object>.Failure("Target student account profile record was not found.", 404);

            await _accountRepository.ReactivateStudentAccountAsync(studentId);
            return ServiceResult<object>.Success(new { Message = "Student account profile has been reactivated successfully." }, 200);
        }
    }
}
