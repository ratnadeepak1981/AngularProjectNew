using CampusServicesPortal.DTOs.Requests.Auth;
using CampusServicesPortal.DTOs.Requests.Sms;
using CampusServicesPortal.DTOs.Requests.Student;
using CampusServicesPortal.DTOs.Responses.Student;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CampusServicesPortal.Services.Implementations
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly IPasswordRepository _passwordRepository;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _memoryCache;

        public StudentService(
            IStudentRepository studentRepository, 
            IPasswordRepository passwordRepository,
            ISmsService smsService,
            IEmailService emailService,
            IMemoryCache memoryCache)
        {
            _studentRepository = studentRepository;
            _passwordRepository = passwordRepository;
            _smsService = smsService;
            _emailService = emailService;
            _memoryCache = memoryCache;
        }

        private static string NormalizePhoneKey(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : Regex.Replace(raw, @"[^\d]", "");
        }

        // POST /api/students/register [BRD Section 4 - Module 1]
        public async Task<ServiceResult<StudentProfileResponseDto>> RegisterStudentAsync(RegisterStudentRequestDto request)
        {
            // 1. Gate check: Verify if index number exists in the pre-approved university registrar file [BRD Rule 5]
            var masterRecord = await _studentRepository.GetMasterRecordAsync(request.IndexNumber);
            if (masterRecord == null)
            {
                return ServiceResult<StudentProfileResponseDto>.Failure("Registration rejected: Index number is not found in the university master list.", 400);
            }

            // 2. Duplication check: Verify if an account has already claimed this index number [BRD Section 4]
            bool isIndexTaken = await _studentRepository.IsIndexRegisteredAsync(request.IndexNumber);
            if (isIndexTaken)
            {
                return ServiceResult<StudentProfileResponseDto>.Failure("Registration rejected: An account has already been registered with this index number.", 409);
            }

            // 3. Email check: Enforce parameter uniqueness across data records [BRD Section 4]
            bool isEmailTaken = await _studentRepository.IsEmailRegisteredAsync(request.Email);
            if (isEmailTaken)
            {
                return ServiceResult<StudentProfileResponseDto>.Failure("Registration rejected: Email address is already in use.", 409);
            }

            // 3.1 Phone check: Enforce phone uniqueness across accounts
            string inputPhone = request.PhoneNumbers?.FirstOrDefault(p => p.IsPrimary)?.PhoneNumber
                ?? request.ContactDetails;
            if (!string.IsNullOrWhiteSpace(inputPhone) && await _studentRepository.IsPhoneRegisteredAsync(inputPhone.Trim()))
            {
                return ServiceResult<StudentProfileResponseDto>.Failure("Registration rejected: Telephone number is already registered under another account profile.", 409);
            }

            // 3.5 Password Policy Validation
            var minLengthSetting = await _passwordRepository.GetSystemSettingAsync("MinPasswordLength");
            int minLength = minLengthSetting != null && int.TryParse(minLengthSetting.SettingValue, out var ml) ? ml : 8;

            var complexitySetting = await _passwordRepository.GetSystemSettingAsync("RequirePasswordComplexity");
            string complexityTier = complexitySetting?.SettingValue ?? "strong";

            var rawPassword = request.Password ?? string.Empty;
            if (rawPassword.Length < minLength)
            {
                return ServiceResult<StudentProfileResponseDto>.Failure($"Password policy error: Password must be at least {minLength} characters long.", 400);
            }

            if (!ValidatePasswordComplexity(rawPassword, complexityTier, minLength, out var complexityErrorMessage))
            {
                return ServiceResult<StudentProfileResponseDto>.Failure($"Password complexity error: {complexityErrorMessage}", 400);
            }

            // 4. Secure Hashing: Generate cryptographically safe salted password hash [BRD Section 10]
            string securePasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 5. Build structured phone numbers
            var phoneEntities = new List<StudentPhoneNumber>();
            if (request.PhoneNumbers != null && request.PhoneNumbers.Count > 0)
            {
                foreach (var p in request.PhoneNumbers)
                {
                    if (!string.IsNullOrWhiteSpace(p.PhoneNumber))
                    {
                        phoneEntities.Add(new StudentPhoneNumber
                        {
                            PhoneType = p.PhoneType,
                            PhoneNumber = p.PhoneNumber.Trim(),
                            IsPrimary = p.IsPrimary,
                            IsVerified = false // Must be verified via OTP
                        });
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.ContactDetails))
            {
                phoneEntities.Add(new StudentPhoneNumber
                {
                    PhoneType = "Primary Mobile",
                    PhoneNumber = request.ContactDetails.Trim(),
                    IsPrimary = true,
                    IsVerified = false
                });
            }

            // Build structured addresses
            var addressEntities = new List<StudentAddress>();
            if (request.Addresses != null && request.Addresses.Count > 0)
            {
                foreach (var a in request.Addresses)
                {
                    if (!string.IsNullOrWhiteSpace(a.AddressLine1))
                    {
                        addressEntities.Add(new StudentAddress
                        {
                            AddressType = a.AddressType,
                            AddressLine1 = a.AddressLine1.Trim(),
                            AddressLine2 = a.AddressLine2?.Trim(),
                            City = a.City.Trim(),
                            DistrictOrProvince = a.DistrictOrProvince?.Trim(),
                            PostalCode = a.PostalCode?.Trim(),
                            Country = string.IsNullOrWhiteSpace(a.Country) ? "Sri Lanka" : a.Country.Trim(),
                            IsPrimary = a.IsPrimary
                        });
                    }
                }
            }

            var primaryPhone = phoneEntities.FirstOrDefault(p => p.IsPrimary) ?? phoneEntities.FirstOrDefault();
            string primaryPhoneNumber = primaryPhone?.PhoneNumber?.Trim() 
                ?? (!string.IsNullOrWhiteSpace(request.ContactDetails) ? request.ContactDetails.Trim() : string.Empty);

            // 6. Instantiate structural database fields
            var newStudent = new CampusServicesPortal.Models.Student
            {
                IndexNumber = masterRecord.IndexNumber,
                FullName = masterRecord.FullName,
                ContactDetails = primaryPhoneNumber,
                EmailVerified = false,
                FacultyId = request.FacultyId,
                EmailVerificationToken = Guid.NewGuid().ToString(),
                EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
                DeactivatedAt = null,
                PhoneNumbers = phoneEntities,
                Addresses = addressEntities,

                User = new CampusServicesPortal.Models.User
                {
                    Email = request.Email,
                    PasswordHash = securePasswordHash,
                    Role = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _studentRepository.AddStudentAsync(newStudent);
            await _studentRepository.SaveChangesAsync();

            // 7. Dispatch Initial Registration SMS OTP Simulation
            if (primaryPhone != null)
            {
                string regOtp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                string cleanPhone = NormalizePhoneKey(primaryPhone.PhoneNumber);
                var cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3) };

                if (!string.IsNullOrEmpty(cleanPhone))
                {
                    _memoryCache.Set($"PhoneOtp_Phone_{cleanPhone}", regOtp, cacheOptions);
                }
                if (!string.IsNullOrEmpty(newStudent.IndexNumber))
                {
                    _memoryCache.Set($"PhoneOtp_User_{newStudent.IndexNumber.Trim().ToLowerInvariant()}", regOtp, cacheOptions);
                }

                await _smsService.DispatchSmsAsync(new SendSmsRequestDto
                {
                    PhoneNumber = primaryPhone.PhoneNumber,
                    Purpose = SmsPurposes.RegistrationOtp,
                    OtpCode = regOtp
                });
            }

            // 8. Dispatch Real Verification Email via SMTP
            try
            {
                var previewResult = await _emailService.GenerateVerificationEmailPreviewAsync(request.Email.Trim());
                if (previewResult.IsSuccess && !string.IsNullOrEmpty(previewResult.Data))
                {
                    await _emailService.SendEmailAsync(
                        request.Email.Trim(),
                        "Campus Services Portal - Verify Your Student Email Account",
                        previewResult.Data
                    );
                }
            }
            catch {}

            // 9. Response Mapping
            var profileResponse = MapToProfileResponseDto(newStudent, request.Email);
            return ServiceResult<StudentProfileResponseDto>.Success(profileResponse, 201);
        }

        // GET /api/students/{id} [BRD Section 4 - Module 1]
        public async Task<ServiceResult<StudentProfileResponseDto>> GetStudentProfileAsync(int id)
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                return ServiceResult<StudentProfileResponseDto>.Failure("Student profile not found.", 404);
            }

            var response = MapToProfileResponseDto(student, student.User?.Email ?? string.Empty);
            return ServiceResult<StudentProfileResponseDto>.Success(response, 200);
        }

        // PUT /api/students/{id} [BRD Section 4 - Module 1]
        public async Task<ServiceResult<StudentProfileResponseDto>> UpdateStudentProfileAsync(int id, UpdateStudentProfileDto request)
        {
            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                return ServiceResult<StudentProfileResponseDto>.Failure("Student profile not found.", 404);
            }

            // 1. Check if Primary Mobile Number was changed
            static string NormalizePhone(string p) => string.IsNullOrWhiteSpace(p) 
                ? string.Empty 
                : p.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();

            var existingPrimary = student.PhoneNumbers.FirstOrDefault(p => p.IsPrimary)?.PhoneNumber?.Trim() 
                ?? student.ContactDetails?.Split('|').FirstOrDefault()?.Split(':').LastOrDefault()?.Trim() 
                ?? string.Empty;
            var newPrimary = request.PhoneNumbers?.FirstOrDefault(p => p.IsPrimary)?.PhoneNumber?.Trim() ?? string.Empty;

            bool isPrimaryMobileChanged = !string.IsNullOrWhiteSpace(existingPrimary) 
                && !string.IsNullOrWhiteSpace(newPrimary) 
                && !NormalizePhone(newPrimary).Equals(NormalizePhone(existingPrimary), StringComparison.OrdinalIgnoreCase);

            if (isPrimaryMobileChanged)
            {
                string inputOtp = request.MobileOtpCode?.Trim() ?? string.Empty;
                string cleanNewPhone = NormalizePhoneKey(newPrimary);
                string userKey = student.IndexNumber.Trim().ToLowerInvariant();

                bool validOtp = false;
                if (!string.IsNullOrEmpty(cleanNewPhone) && _memoryCache.TryGetValue($"PhoneOtp_Phone_{cleanNewPhone}", out string? cachedPhoneOtp) && cachedPhoneOtp == inputOtp)
                {
                    validOtp = true;
                    _memoryCache.Remove($"PhoneOtp_Phone_{cleanNewPhone}");
                }
                else if (_memoryCache.TryGetValue($"PhoneOtp_User_{userKey}", out string? cachedUserOtp) && cachedUserOtp == inputOtp)
                {
                    validOtp = true;
                    _memoryCache.Remove($"PhoneOtp_User_{userKey}");
                }

                if (!validOtp)
                {
                    return ServiceResult<StudentProfileResponseDto>.Failure("OTP verification required: Please verify the dynamic OTP sent to your new primary mobile number.", 400);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                student.FullName = request.FullName.Trim();
            }

            student.FacultyId = request.FacultyId;

            // 2. Sync Phone Numbers
            if (request.PhoneNumbers != null && request.PhoneNumbers.Count > 0)
            {
                // Validate duplicate phone numbers across other student profiles
                foreach (var p in request.PhoneNumbers)
                {
                    if (!string.IsNullOrWhiteSpace(p.PhoneNumber))
                    {
                        if (await _studentRepository.IsPhoneRegisteredAsync(p.PhoneNumber.Trim(), id))
                        {
                            return ServiceResult<StudentProfileResponseDto>.Failure($"Telephone number '{p.PhoneNumber.Trim()}' is already registered under another account profile.", 409);
                        }
                    }
                }

                var updatedPhones = new List<StudentPhoneNumber>();
                foreach (var p in request.PhoneNumbers)
                {
                    if (!string.IsNullOrWhiteSpace(p.PhoneNumber))
                    {
                        bool wasVerified = p.IsPrimary 
                            ? (isPrimaryMobileChanged ? true : (student.PhoneNumbers.FirstOrDefault(x => x.IsPrimary)?.IsVerified ?? false))
                            : p.IsVerified;

                        updatedPhones.Add(new StudentPhoneNumber
                        {
                            Id = p.Id ?? 0,
                            StudentId = id,
                            PhoneType = p.PhoneType,
                            PhoneNumber = p.PhoneNumber.Trim(),
                            IsPrimary = p.IsPrimary,
                            IsVerified = wasVerified
                        });
                    }
                }
                await _studentRepository.SyncPhoneNumbersAsync(id, updatedPhones);
                var primPhone = updatedPhones.FirstOrDefault(p => p.IsPrimary) ?? updatedPhones.FirstOrDefault();
                student.ContactDetails = primPhone?.PhoneNumber?.Trim() ?? string.Empty;
            }

            // 3. Sync Addresses
            if (request.Addresses != null)
            {
                var updatedAddresses = new List<StudentAddress>();
                foreach (var a in request.Addresses)
                {
                    if (!string.IsNullOrWhiteSpace(a.AddressLine1))
                    {
                        updatedAddresses.Add(new StudentAddress
                        {
                            Id = a.Id ?? 0,
                            StudentId = id,
                            AddressType = a.AddressType,
                            AddressLine1 = a.AddressLine1.Trim(),
                            AddressLine2 = a.AddressLine2?.Trim(),
                            City = a.City.Trim(),
                            DistrictOrProvince = a.DistrictOrProvince?.Trim(),
                            PostalCode = a.PostalCode?.Trim(),
                            Country = string.IsNullOrWhiteSpace(a.Country) ? "Sri Lanka" : a.Country.Trim(),
                            IsPrimary = a.IsPrimary
                        });
                    }
                }
                await _studentRepository.SyncAddressesAsync(id, updatedAddresses);
            }

            await _studentRepository.UpdateAsync(student);
            await _studentRepository.SaveChangesAsync();

            return await GetStudentProfileAsync(id);
        }

        // GET /api/students?search=&faculty= [BRD Section 4 - Module 1]
        public async Task<ServiceResult<IEnumerable<StudentProfileResponseDto>>> SearchStudentsAsync(string? search, string? faculty)
        {
            var students = await _studentRepository.SearchStudentsAsync(search, faculty);
            var resultList = students.Select(s => MapToProfileResponseDto(s, s.User?.Email ?? string.Empty)).ToList();
            return ServiceResult<IEnumerable<StudentProfileResponseDto>>.Success(resultList, 200);
        }

        private static StudentProfileResponseDto MapToProfileResponseDto(Student student, string email)
        {
            var phoneDtos = student.PhoneNumbers.Select(p => new StudentPhoneNumberDto
            {
                Id = p.Id,
                PhoneType = p.PhoneType,
                PhoneNumber = p.PhoneNumber,
                IsPrimary = p.IsPrimary,
                IsVerified = p.IsVerified
            }).ToList();

            var addressDtos = student.Addresses.Select(a => new StudentAddressDto
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
            }).ToList();

            return new StudentProfileResponseDto
            {
                Id = student.Id,
                IndexNumber = student.IndexNumber,
                Name = student.FullName,
                Email = email,
                ContactDetails = student.ContactDetails,
                EmailVerified = student.EmailVerified,
                PhoneVerified = student.PhoneNumbers.Any(p => p.IsPrimary && p.IsVerified),
                IsActive = !student.DeactivatedAt.HasValue && (student.User == null || student.User.IsActive),
                FacultyId = student.FacultyId,
                FacultyName = student.Faculty?.Name ?? "Unassigned",
                PhoneNumbers = phoneDtos,
                Addresses = addressDtos
            };
        }

        // DELETE /api/students/{id} (Deactivate Action) [BRD Rule 11]
        public async Task<ServiceResult<bool>> DeactivateStudentAsync(int id)
        {
            // 1. Core Rule Check: Safely inspect ongoing obligations across blocks before deactivation [BRD Rule 11]
            if (await _studentRepository.HasActiveHostelAllocationAsync(id) ||
                await _studentRepository.HasUpcomingLabBookingsAsync(id) ||
                await _studentRepository.HasUpcomingEventRegistrationsAsync(id) ||
                await _studentRepository.HasPendingCertificateRequestAsync(id) ||
                await _studentRepository.HasUnpaidFeesAsync(id))
            {
                return ServiceResult<bool>.Failure("Cannot deactivate student with active items.", 400);
            }


            var student = await _studentRepository.GetByIdAsync(id);
            if (student == null)
            {
                return ServiceResult<bool>.Failure("Student not found.", 404);
            }

            student.DeactivatedAt = DateTime.UtcNow;
            if (student.User != null)
            {
                student.User.IsActive = false; // Performs soft deactivation across identity mapping
            }

            await _studentRepository.UpdateAsync(student);
            await _studentRepository.SaveChangesAsync();

            return ServiceResult<bool>.Success(true, 200);
        }

        // GET /api/student-master/{indexNumber} Validation & Pre-fill Logic [BRD Page 4]
        public async Task<ServiceResult<object>> VerifyMasterIndexAsync(string indexNumber)
        {
            var decodedIndex = Uri.UnescapeDataString(indexNumber);
            var masterRecord = await _studentRepository.GetMasterRecordAsync(decodedIndex);

            // Rule 1: Reject registration if index number is not in master list [Index 0.1.4]
            if (masterRecord == null)
            {
                return ServiceResult<object>.Failure("Registration rejected: Index number is not found in the university master list.", 404);
            }

            // Rule 2: Master records can only be linked to one single student profile [Index 0.1.4]
            bool isAlreadyRegistered = await _studentRepository.IsIndexRegisteredAsync(decodedIndex);
            if (isAlreadyRegistered)
            {
                return ServiceResult<object>.Failure("Registration rejected: This index number has already been used to create an account.", 400);
            }

            // Return fields safely to allow client-side pre-fill automation [Index 0.1.4]
            return ServiceResult<object>.Success(new
            {
                indexNumber = masterRecord.IndexNumber,
                fullName = masterRecord.FullName,
                facultyId = masterRecord.FacultyId
            }, 200);
        }

        // GET /api/student-master?search= Admin Verification Module [BRD Page 4]
        public async Task<ServiceResult<IEnumerable<object>>> SearchMasterRecordsAsync(string? search)
        {
            var records = await _studentRepository.SearchMasterListAsync(search);
            var registeredIndices = await _studentRepository.GetRegisteredIndexNumbersAsync();

            var result = records.Select(r => new
            {
                r.Id,
                r.IndexNumber,
                r.FullName,
                r.FacultyId,
                IsUsed = registeredIndices.Contains(r.IndexNumber.ToLower())
            });

            return ServiceResult<IEnumerable<object>>.Success(result, 200);
        }

        // POST /api/student-master/import Administrative Stream Parser [BRD Page 4]
        public async Task<ServiceResult<int>> BulkImportMasterRecordsAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return ServiceResult<int>.Failure("Please upload a valid CSV file data payload.", 400);
            }

            var masterRecordsList = new List<StudentMasterList>();

            try
            {
                using var reader = new System.IO.StreamReader(file.OpenReadStream());
                // Skip structural CSV text metadata header row
                await reader.ReadLineAsync();

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var cleanLine = line.TrimEnd('\r', '\n');
                    var parts = cleanLine.Split(',');
                    if (parts.Length >= 3 && int.TryParse(parts[2].Trim('"', '\'', ' ', '\t', '\r', '\n'), out int facId))
                    {
                        var cleanIndex = parts[0].Trim('"', '\'', ' ', '\t', '\r', '\n', '\uFEFF');
                        var cleanName = parts[1].Trim('"', '\'', ' ', '\t', '\r', '\n');
                        if (!string.IsNullOrWhiteSpace(cleanIndex) && !string.IsNullOrWhiteSpace(cleanName))
                        {
                            masterRecordsList.Add(new StudentMasterList
                            {
                                IndexNumber = cleanIndex,
                                FullName = cleanName,
                                FacultyId = facId
                            });
                        }
                    }
                }

                int insertedCount = await _studentRepository.BulkImportMasterListAsync(masterRecordsList);
                if (insertedCount > 0)
                {
                    try
                    {
                        await _studentRepository.SaveChangesAsync();
                    }
                    catch (Exception ex) when (ex is Microsoft.EntityFrameworkCore.DbUpdateException || ex is CampusServicesPortal.Exceptions.DuplicateBookingException)
                    {
                        return ServiceResult<int>.Success(0, 200);
                    }
                }

                return ServiceResult<int>.Success(insertedCount, 200);
            }
            catch (Exception ex)
            {
                string errorDetail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return ServiceResult<int>.Failure($"An error occurred during file parsing: {errorDetail}", 400);
            }
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
