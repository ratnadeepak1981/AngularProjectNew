using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Auth;
using CampusServicesPortal.DTOs.Responses.Auth;
using CampusServicesPortal.DTOs.Responses.Student;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CampusServicesPortal.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _config;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public AuthService(
            IAuthRepository authRepository,
            IConfiguration config,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _authRepository = authRepository;
            _config = config;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request)
        {
            var user = await _authRepository.GetUserByEmailAsync(request.Email);

            // 1. Account Lockout Check
            if (user != null && user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
            {
                int remainingMinutes = Math.Max(1, (int)Math.Ceiling((user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes));
                await _auditLogService.LogActivityAsync(
                    userId: user.Id,
                    userDisplayName: user.Email,
                    action: "AccountLockoutActive",
                    module: "Auth",
                    entityId: user.Id.ToString(),
                    description: $"Login attempt blocked: Account is locked out for {remainingMinutes} more minute(s).",
                    isSuccess: false);

                return ServiceResult<AuthResponseDto>.Failure($"Account is temporarily locked due to multiple failed login attempts. Please try again in {remainingMinutes} minute(s).", 423);
            }

            if (user == null || !VerifyPasswordHash(request.Password, user.PasswordHash))
            {
                if (user != null)
                {
                    user.FailedLoginAttempts++;
                    var maxFailedSetting = await _authRepository.GetSystemSettingAsync("MaxFailedLogins");
                    int maxFailed = maxFailedSetting != null && int.TryParse(maxFailedSetting.SettingValue, out int mf) ? mf : 5;

                    var lockoutMinsSetting = await _authRepository.GetSystemSettingAsync("AccountLockoutDurationMinutes");
                    int lockoutMins = lockoutMinsSetting != null && int.TryParse(lockoutMinsSetting.SettingValue, out int lm) ? lm : 15;

                    if (user.FailedLoginAttempts >= maxFailed)
                    {
                        user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(lockoutMins);
                        await _auditLogService.LogActivityAsync(
                            userId: user.Id,
                            userDisplayName: user.Email,
                            action: "AccountLockedOut",
                            module: "Auth",
                            entityId: user.Id.ToString(),
                            description: $"Account locked for {lockoutMins} minutes due to {user.FailedLoginAttempts} consecutive failed attempts.",
                            isSuccess: false);
                    }

                    await _authRepository.UpdateUserAsync(user);
                }

                await _auditLogService.LogActivityAsync(
                    userId: user?.Id,
                    userDisplayName: request.Email,
                    action: "LoginFailure",
                    module: "Auth",
                    entityId: user?.Id.ToString(),
                    description: $"Failed login attempt for '{request.Email}': Invalid credentials provided.",
                    isSuccess: false);

                if (user != null)
                {
                    var st = await _authRepository.GetStudentByUserIdWithFacultyAsync(user.Id);
                    if (st != null)
                    {
                        await _notificationService.SendInternalNotificationAsync(new DTOs.Requests.Nortifcation.CreateNotificationDto
                        {
                            StudentId = st.Id,
                            Type = "SecurityAlert",
                            Message = $"Security Alert: Failed login attempt for '{request.Email}' (Index: {st.IndexNumber}). Invalid password entered."
                        });
                    }
                }

                return ServiceResult<AuthResponseDto>.Failure("Invalid login credentials provided.", 401);
            }

            // 2. Check Temporary Password Expiration
            if (user.MustChangePassword && user.TemporaryPasswordExpiresAt.HasValue && user.TemporaryPasswordExpiresAt.Value < DateTime.UtcNow)
            {
                await _auditLogService.LogActivityAsync(
                    userId: user.Id,
                    userDisplayName: user.Email,
                    action: "TemporaryPasswordExpired",
                    module: "Auth",
                    entityId: user.Id.ToString(),
                    description: $"Login rejected for '{user.Email}': Temporary password credential has expired.",
                    isSuccess: false);

                return ServiceResult<AuthResponseDto>.Failure("Your temporary password has expired. Please request a password reset or contact administration.", 401);
            }

            // 3. Reset failed login attempts on successful authentication
            if (user.FailedLoginAttempts > 0 || user.LockoutEndUtc.HasValue)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEndUtc = null;
                await _authRepository.UpdateUserAsync(user);
            }

            var student = await _authRepository.GetStudentByUserIdWithFacultyAsync(user.Id);

            if (user.Role == "Student" && student != null)
            {
                if (!student.EmailVerified)
                {
                    await _auditLogService.LogActivityAsync(
                        userId: user.Id,
                        userDisplayName: user.Email,
                        action: "UnverifiedEmailLogin",
                        module: "Auth",
                        entityId: user.Id.ToString(),
                        description: $"Login rejected for student '{user.Email}': Email address has not been verified.",
                        isSuccess: false);

                    return ServiceResult<AuthResponseDto>.Failure("Access Denied. Your email address has not been verified yet.", 403);
                }

                if (student.DeactivatedAt.HasValue || !user.IsActive)
                {
                    await _auditLogService.LogActivityAsync(
                        userId: user.Id,
                        userDisplayName: user.Email,
                        action: "DeactivatedAccountLogin",
                        module: "Auth",
                        entityId: user.Id.ToString(),
                        description: $"Login rejected for account '{user.Email}': Student portal account has been deactivated.",
                        isSuccess: false);

                    await _notificationService.SendInternalNotificationAsync(new DTOs.Requests.Nortifcation.CreateNotificationDto
                    {
                        StudentId = student.Id,
                        Type = "SecurityAlert",
                        Message = $"Security Alert: Unauthorized access attempt blocked for deactivated student '{user.Email}' (Index: {student.IndexNumber})."
                    });

                    return ServiceResult<AuthResponseDto>.Failure("Authentication Failed. This student portal account has been deactivated.", 403);
                }
            }

            // 4. Check Password Expiry Policy
            var expiryDaysSetting = await _authRepository.GetSystemSettingAsync("PasswordExpiryDays");
            int expiryDays = expiryDaysSetting != null && int.TryParse(expiryDaysSetting.SettingValue, out int ed) ? ed : 90;
            bool isPasswordExpired = false;
            if (expiryDays > 0 && user.LastPasswordChangedAt.HasValue && user.LastPasswordChangedAt.Value.AddDays(expiryDays) < DateTime.UtcNow)
            {
                isPasswordExpired = true;
            }

            bool mustChange = user.MustChangePassword || isPasswordExpired;
            string? changeReason = null;
            if (user.MustChangePassword) changeReason = "TemporaryPassword";
            else if (isPasswordExpired) changeReason = "ExpiredPassword";

            var tokenString = GenerateJwtToken(user, student?.Id ?? 0);
            var refreshTokenString = GenerateSecureRandomToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _authRepository.SaveRefreshTokenAsync(refreshTokenEntity);

            var response = new AuthResponseDto
            {
                Token = tokenString,
                RefreshToken = refreshTokenString,
                Role = user.Role,
                MustChangePassword = mustChange,
                PasswordExpired = isPasswordExpired,
                ForceChangeReason = changeReason,
                Profile = new StudentProfileResponseDto
                {
                    Id = student?.Id ?? 0,
                    IndexNumber = student?.IndexNumber ?? "N/A",
                    Name = student?.FullName ?? (!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email),
                    Email = user.Email,
                    FacultyName = student?.Faculty?.Name ?? "Central Administration",
                    ContactDetails = student?.ContactDetails,
                    EmailVerified = student?.EmailVerified ?? false,
                    PhoneVerified = student?.PhoneNumbers.Any(p => p.IsPrimary && p.IsVerified) ?? false,
                    IsActive = !student?.DeactivatedAt.HasValue ?? user.IsActive,
                    PhoneNumbers = student?.PhoneNumbers.Select(p => new DTOs.Requests.Student.StudentPhoneNumberDto
                    {
                        Id = p.Id,
                        PhoneType = p.PhoneType,
                        PhoneNumber = p.PhoneNumber,
                        IsPrimary = p.IsPrimary,
                        IsVerified = p.IsVerified
                    }).ToList() ?? new(),
                    Addresses = student?.Addresses.Select(a => new DTOs.Requests.Student.StudentAddressDto
                    {
                        Id = a.Id,
                        AddressType = a.AddressType,
                        AddressLine1 = a.AddressLine1,
                        AddressLine2 = a.AddressLine2,
                        City = a.City,
                        DistrictOrProvince = a.DistrictOrProvince,
                        PostalCode = a.PostalCode,
                        Country = a.Country,
                        IsPrimary = a.IsPrimary
                    }).ToList() ?? new()
                }
            };

            return ServiceResult<AuthResponseDto>.Success(response, 200);
        }

        public async Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var existingToken = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);

            if (existingToken == null)
            {
                await _auditLogService.LogActivityAsync(
                    userId: null,
                    userDisplayName: "Unknown Token Principal",
                    action: "RefreshTokenFailure",
                    module: "Auth",
                    entityId: null,
                    description: "Token refresh failed: Invalid refresh token supplied.",
                    isSuccess: false);

                return ServiceResult<AuthResponseDto>.Failure("Invalid refresh token supplied.", 401);
            }

            if (!existingToken.IsActive)
            {
                await _auditLogService.LogActivityAsync(
                    userId: existingToken.UserId,
                    userDisplayName: existingToken.User?.Email ?? "Token User",
                    action: "RefreshTokenFailure",
                    module: "Auth",
                    entityId: existingToken.UserId.ToString(),
                    description: "Token refresh failed: Expired or revoked refresh token.",
                    isSuccess: false);

                return ServiceResult<AuthResponseDto>.Failure("Expired or revoked refresh token. Please sign in again.", 401);
            }

            var user = existingToken.User;
            if (user == null || !user.IsActive)
            {
                await _auditLogService.LogActivityAsync(
                    userId: user?.Id,
                    userDisplayName: user?.Email ?? "Unknown User",
                    action: "DeactivatedAccountTokenRefresh",
                    module: "Auth",
                    entityId: user?.Id.ToString(),
                    description: $"Token refresh rejected: Associated user account '{user?.Email}' is deactivated.",
                    isSuccess: false);

                return ServiceResult<AuthResponseDto>.Failure("Associated user account is deactivated.", 403);
            }

            var student = await _authRepository.GetStudentByUserIdWithFacultyAsync(user.Id);
            if (user.Role == "Student" && student != null && student.DeactivatedAt.HasValue)
            {
                await _auditLogService.LogActivityAsync(
                    userId: user.Id,
                    userDisplayName: user.Email,
                    action: "DeactivatedAccountTokenRefresh",
                    module: "Auth",
                    entityId: user.Id.ToString(),
                    description: $"Token refresh rejected: Associated student account '{user.Email}' is deactivated.",
                    isSuccess: false);

                return ServiceResult<AuthResponseDto>.Failure("Associated student account has been deactivated.", 403);
            }

            // Execute Refresh Token Rotation: Revoke old token and issue new token pair
            var newRefreshTokenString = GenerateSecureRandomToken();
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.ReplacedByToken = newRefreshTokenString;

            await _authRepository.UpdateRefreshTokenAsync(existingToken);

            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _authRepository.SaveRefreshTokenAsync(newRefreshTokenEntity);

            var newJwtToken = GenerateJwtToken(user, student?.Id ?? 0);

            var response = new AuthResponseDto
            {
                Token = newJwtToken,
                RefreshToken = newRefreshTokenString,
                Role = user.Role,
                Profile = new StudentProfileResponseDto
                {
                    Id = student?.Id ?? 0,
                    IndexNumber = student?.IndexNumber ?? "N/A",
                    Name = student?.FullName ?? (!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email),
                    Email = user.Email,
                    FacultyName = student?.Faculty?.Name ?? "Central Administration",
                    ContactDetails = student?.ContactDetails,
                    EmailVerified = student?.EmailVerified ?? false,
                    PhoneVerified = student?.PhoneNumbers.Any(p => p.IsPrimary && p.IsVerified) ?? false,
                    IsActive = !student?.DeactivatedAt.HasValue ?? user.IsActive,
                    PhoneNumbers = student?.PhoneNumbers.Select(p => new DTOs.Requests.Student.StudentPhoneNumberDto
                    {
                        Id = p.Id,
                        PhoneType = p.PhoneType,
                        PhoneNumber = p.PhoneNumber,
                        IsPrimary = p.IsPrimary,
                        IsVerified = p.IsVerified
                    }).ToList() ?? new(),
                    Addresses = student?.Addresses.Select(a => new DTOs.Requests.Student.StudentAddressDto
                    {
                        Id = a.Id,
                        AddressType = a.AddressType,
                        AddressLine1 = a.AddressLine1,
                        AddressLine2 = a.AddressLine2,
                        City = a.City,
                        DistrictOrProvince = a.DistrictOrProvince,
                        PostalCode = a.PostalCode,
                        Country = a.Country,
                        IsPrimary = a.IsPrimary
                    }).ToList() ?? new()
                }
            };

            return ServiceResult<AuthResponseDto>.Success(response, 200);
        }

        public async Task<ServiceResult<bool>> RevokeTokenAsync(RevokeTokenRequestDto request)
        {
            var tokenEntity = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);
            if (tokenEntity == null || !tokenEntity.IsActive)
                return ServiceResult<bool>.Failure("Invalid or already revoked refresh token.", 400);

            tokenEntity.RevokedAt = DateTime.UtcNow;
            await _authRepository.UpdateRefreshTokenAsync(tokenEntity);

            return ServiceResult<bool>.Success(true, 200);
        }

        private string GenerateJwtToken(User user, int studentId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Secret Key is not configured."));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Role == "Student" ? studentId.ToString() : user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(60), // standard access token window
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateSecureRandomToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                // If storedHash is a valid BCrypt hash format (starts with $2 and at least 60 chars)
                if (storedHash.StartsWith("$2") && storedHash.Length >= 60)
                {
                    return BCrypt.Net.BCrypt.Verify(password, storedHash);
                }

                // Fail-safe check for plain-text passwords or legacy test hashes in database
                if (storedHash == password)
                {
                    return true;
                }

                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch (Exception)
            {
                // Fallback comparison for unhashed legacy text stored in database
                return storedHash == password;
            }
        }
    }
}
