using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Billing; // Matches your exact request DTO namespace [INDEX]
using CampusServicesPortal.DTOs.Responses.Billing;
using CampusServicesPortal.DTOs.Requests.Nortifcation; // Maps cleanly to your CreateNotificationDto parameter model [INDEX]
using CampusServicesPortal.DTOs.Requests.Sms;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.Extensions.Caching.Memory;

namespace CampusServicesPortal.Services.Implementations
{
    public class BillingService : IBillingService
    {
        private readonly IBillingRepository _billingRepo;
        private readonly INotificationService _notificationService;
        private readonly IStudentRepository _studentRepo;
        private readonly ISmsService _smsService;
        private readonly IPasswordRepository _passwordRepo;
        private readonly IMemoryCache _memoryCache;

        public BillingService(
            IBillingRepository billingRepo, 
            INotificationService notificationService,
            IStudentRepository studentRepo,
            ISmsService smsService,
            IPasswordRepository passwordRepo,
            IMemoryCache memoryCache)
        {
            _billingRepo = billingRepo;
            _notificationService = notificationService;
            _studentRepo = studentRepo;
            _smsService = smsService;
            _passwordRepo = passwordRepo;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// POST: Maps AssignFeeDto to distribute either individual student billing records or faculty group bulk arrays.
        /// </summary>
        public async Task<ServiceResult<object>> AssignFeeAsync(AssignFeeDto request)
        {
            if (!request.StudentId.HasValue && !request.FacultyId.HasValue)
            {
                return ServiceResult<object>.Failure("Invalid Assignment Scope. Supply either a StudentId or a FacultyId.", 400);
            }

            string formattedAmount = request.Amount.ToString("N2");
            string billingPeriodClean = request.BillingPeriod.Trim();
            string? descriptionClean = request.Description?.Trim();

            if (request.StudentId.HasValue)
            {
                int studentId = request.StudentId.Value;

                bool isDuplicate = await _billingRepo.HasDuplicateUnpaidFeeAsync(studentId, request.FeeTypeId, billingPeriodClean);
                if (isDuplicate)
                {
                    return ServiceResult<object>.Failure($"Duplicate Invoice Rejected. An outstanding invoice already exists for this student for period '{billingPeriodClean}'.", 400);
                }

                var feePayment = new FeePayment
                {
                    StudentId = studentId,
                    FeeTypeId = request.FeeTypeId,
                    Amount = request.Amount,
                    BillingPeriod = billingPeriodClean,
                    Description = descriptionClean,
                    Status = "Outstanding"
                };

                await _billingRepo.AddFeePaymentAsync(feePayment);

                // AUTOMATED TRIGGER: Stage single-student notification inside the transaction context cache [INDEX]
                await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
                {
                    StudentId = studentId,
                    Type = "NewFeeAssigned",
                    Message = $"New Invoice Issued: An outstanding university charge (Period: {billingPeriodClean}) amounting to LKR {formattedAmount} has been posted to your account."
                });

                await _billingRepo.SaveChangesAsync();

                var completeRecord = await _billingRepo.GetFeePaymentByIdAsync(feePayment.Id);
                return ServiceResult<object>.Success(MapToResponseDto(completeRecord!), 201);
            }
            else
            {
                int facultyId = request.FacultyId!.Value;

                var studentCohortList = await _billingRepo.GetStudentsByFacultyIdAsync(facultyId);
                if (studentCohortList == null || !studentCohortList.Any())
                {
                    return ServiceResult<object>.Success(new
                    {
                        Message = "The selected faculty currently has no active enrolled students. 0 fee assignments were created.",
                        AssignedCount = 0,
                        SkippedCount = 0
                    }, 200);
                }

                int assignedCount = 0;
                int skippedCount = 0;

                foreach (var student in studentCohortList)
                {
                    bool hasDuplicate = await _billingRepo.HasDuplicateUnpaidFeeAsync(student.Id, request.FeeTypeId, billingPeriodClean);
                    if (hasDuplicate)
                    {
                        skippedCount++;
                        continue;
                    }

                    var feePayment = new FeePayment
                    {
                        StudentId = student.Id,
                        FeeTypeId = request.FeeTypeId,
                        Amount = request.Amount,
                        BillingPeriod = billingPeriodClean,
                        Description = descriptionClean,
                        Status = "Outstanding"
                    };

                    await _billingRepo.AddFeePaymentAsync(feePayment);
                    assignedCount++;

                    // AUTOMATED TRIGGER: Loop and stage individual notifications for bulk cohorts concurrently [INDEX]
                    await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
                    {
                        StudentId = student.Id,
                        Type = "NewFeeAssigned",
                        Message = $"New Invoice Issued: An outstanding institutional charge (Period: {billingPeriodClean}) has been posted to your account ledger. Amount: LKR {formattedAmount}."
                    });
                }

                await _billingRepo.SaveChangesAsync();
                return ServiceResult<object>.Success(new
                {
                    Message = $"Bulk assignment run completed successfully. Assigned to {assignedCount} student(s)" + (skippedCount > 0 ? $", {skippedCount} skipped due to existing duplicate fee." : "."),
                    AssignedCount = assignedCount,
                    SkippedCount = skippedCount
                }, 201);
            }
        }

        /// <summary>
        /// POST: Generates an individual library/lab penalty sanction charge fine record [INDEX].
        /// </summary>
        public async Task<ServiceResult<object>> IssueLabFineAsync(GenerateLabFineDto request)
        {
            var feePayment = new FeePayment
            {
                StudentId = request.StudentId,
                FeeTypeId = 4, // Defaults to your system's master Lab Fine Type Id database row entry [INDEX]
                Amount = request.Amount,
                BillingPeriod = DateTime.UtcNow.Year.ToString() + " - Fine",
                Description = request.Reason.Trim(), // Maps cleanly to your model's Reason property field
                Status = "Outstanding"
            };

            await _billingRepo.AddFeePaymentAsync(feePayment);

            // AUTOMATED TRIGGER: Stage lab fine alert inside your shared transaction memory pool [INDEX]
            string formattedAmount = request.Amount.ToString("N2");
            await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
            {
                StudentId = request.StudentId, // Securely targets exclusively the single affected student account profile [INDEX]
                Type = "NewFeeAssigned",
                Message = $"Penalty Notice: A laboratory fine amounting to LKR {formattedAmount} has been added to your profile ledger. Reason: \"{feePayment.Description}\"."
            });

            await _billingRepo.SaveChangesAsync();

            var completeRecord = await _billingRepo.GetFeePaymentByIdAsync(feePayment.Id);
            return ServiceResult<object>.Success(MapToResponseDto(completeRecord!), 201);
        }

        private static string NormalizePhoneKey(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : Regex.Replace(raw, @"[^\d]", "");
        }

        private static string MaskPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "Mobile on file";
            string digits = NormalizePhoneKey(phone);
            if (digits.Length >= 9)
            {
                return $"+{digits[..2]} {digits[2..4]} *** {digits[^3..]}";
            }
            return phone;
        }

        private async Task<int> GetOtpValidityMinutesAsync()
        {
            try
            {
                var setting = await _passwordRepo.GetSystemSettingAsync("OtpValidityMinutes");
                if (setting != null && int.TryParse(setting.SettingValue, out int mins) && mins > 0)
                {
                    return mins;
                }
            }
            catch {}
            return 3;
        }

        /// <summary>
        /// POST: Initiates server-authoritative 3D Secure OTP dispatch for student card payment.
        /// </summary>
        public async Task<ServiceResult<object>> RequestPaymentOtpAsync(int paymentId, int studentId)
        {
            var feePayment = await _billingRepo.GetFeePaymentByIdAsync(paymentId);
            if (feePayment == null || feePayment.StudentId != studentId)
            {
                return ServiceResult<object>.Failure("Target statement invoice record not found.", 404);
            }

            if (feePayment.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<object>.Failure("This invoice has already been settled.", 400);
            }

            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student == null)
            {
                return ServiceResult<object>.Failure("Student profile not found.", 404);
            }

            var primaryPhone = student.PhoneNumbers.FirstOrDefault(p => p.IsPrimary);
            string rawPhone = primaryPhone?.PhoneNumber ?? student.ContactDetails ?? string.Empty;

            if (string.IsNullOrWhiteSpace(rawPhone))
            {
                return ServiceResult<object>.Failure("No primary mobile number found on your account. Please update your profile before proceeding with online payment.", 400);
            }

            bool isVerified = primaryPhone?.IsVerified == true || student.PhoneNumbers.Any(p => p.IsVerified);
            if (!isVerified)
            {
                return ServiceResult<object>.Failure("Your primary mobile number is not yet verified. Please verify your mobile line in Profile Settings before proceeding with online payment.", 403);
            }

            int validityMinutes = await GetOtpValidityMinutesAsync();
            string otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");

            // Cache OTP per student & payment
            _memoryCache.Set($"PaymentOtp_{studentId}_{paymentId}", otpCode, TimeSpan.FromMinutes(validityMinutes));

            string cleanPhone = NormalizePhoneKey(rawPhone);
            if (!string.IsNullOrEmpty(cleanPhone))
            {
                _memoryCache.Set($"PaymentOtp_Phone_{cleanPhone}", otpCode, TimeSpan.FromMinutes(validityMinutes));
                _memoryCache.Set($"PaymentAmount_Phone_{cleanPhone}", feePayment.Amount, TimeSpan.FromMinutes(validityMinutes));
                _memoryCache.Set($"PaymentTxn_Phone_{cleanPhone}", $"TXN-{paymentId}", TimeSpan.FromMinutes(validityMinutes));
            }

            // Dispatch SMS Simulation
            await _smsService.DispatchSmsAsync(new SendSmsRequestDto
            {
                PhoneNumber = rawPhone.Trim(),
                Purpose = SmsPurposes.PaymentOtp,
                OtpCode = otpCode,
                Amount = feePayment.Amount,
                TransactionId = $"TXN-{paymentId}"
            });

            return ServiceResult<object>.Success(new
            {
                Message = $"3D Secure Payment OTP dispatched to {MaskPhoneNumber(rawPhone)}. Valid for {validityMinutes} minutes.",
                ValidityMinutes = validityMinutes,
                ExpiresInSeconds = validityMinutes * 60,
                PhoneNumber = MaskPhoneNumber(rawPhone)
            }, 200);
        }

        /// <summary>
        /// PUT/POST: Settles an outstanding fee invoice with backend authorization.
        /// </summary>
        public async Task<ServiceResult<FeePaymentResponseDto>> ProcessPaymentAsync(int paymentId, int studentId, ProcessPaymentRequestDto? request = null)
        {
            var feePayment = await _billingRepo.GetFeePaymentByIdAsync(paymentId);
            if (feePayment == null || feePayment.StudentId != studentId)
            {
                return ServiceResult<FeePaymentResponseDto>.Failure("Target statement invoice record not found.", 404);
            }

            if (feePayment.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<FeePaymentResponseDto>.Failure("Transaction Aborted. This specific invoice balance has already been settled.", 400);
            }

            var student = await _studentRepo.GetByIdAsync(studentId);
            if (student == null)
            {
                return ServiceResult<FeePaymentResponseDto>.Failure("Student profile not found.", 404);
            }

            var primaryPhone = student.PhoneNumbers.FirstOrDefault(p => p.IsPrimary);
            string rawPhone = primaryPhone?.PhoneNumber ?? student.ContactDetails ?? string.Empty;
            bool isVerified = primaryPhone?.IsVerified == true || student.PhoneNumbers.Any(p => p.IsVerified);

            if (!isVerified)
            {
                return ServiceResult<FeePaymentResponseDto>.Failure("Payment Blocked: Your primary mobile number must be verified before settling fees online.", 403);
            }

            string channel = request?.PaymentChannel?.ToLowerInvariant() ?? "card";

            if (channel == "card" || !string.IsNullOrWhiteSpace(request?.OtpCode))
            {
                string inputOtp = request?.OtpCode?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(inputOtp) || inputOtp.Length != 6)
                {
                    return ServiceResult<FeePaymentResponseDto>.Failure("A valid 6-digit 3D Secure OTP code is required to authorize card payment.", 400);
                }

                string? cachedOtp = null;
                string cleanPhone = NormalizePhoneKey(rawPhone);

                if (_memoryCache.TryGetValue($"PaymentOtp_{studentId}_{paymentId}", out string? studentPaymentOtp))
                {
                    cachedOtp = studentPaymentOtp;
                }
                else if (!string.IsNullOrEmpty(cleanPhone) && _memoryCache.TryGetValue($"PaymentOtp_Phone_{cleanPhone}", out string? phoneOtp))
                {
                    cachedOtp = phoneOtp;
                }

                if (string.IsNullOrEmpty(cachedOtp) || cachedOtp != inputOtp)
                {
                    return ServiceResult<FeePaymentResponseDto>.Failure("Invalid or expired 3D Secure Payment OTP code. Please click Resend OTP to receive a fresh code.", 400);
                }

                // Invalidate OTP cache upon successful validation
                _memoryCache.Remove($"PaymentOtp_{studentId}_{paymentId}");
                if (!string.IsNullOrEmpty(cleanPhone))
                {
                    _memoryCache.Remove($"PaymentOtp_Phone_{cleanPhone}");
                }
            }

            // Progress tracking variables inside tracking context memory
            feePayment.Status = "Paid";
            feePayment.PaidAt = DateTime.UtcNow;

            // AUTOMATED TRIGGER: Emit successful settlement transaction alert notification [INDEX]
            string feeName = feePayment.FeeType?.Name ?? "Institutional Charge";
            await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
            {
                StudentId = studentId,
                Type = "FeePaymentSettled",
                Message = $"Payment Confirmed! Your transaction clearing LKR {feePayment.Amount.ToString("N2")} for '{feeName}' has been successfully verified."
            });

            if (!string.IsNullOrWhiteSpace(rawPhone))
            {
                await _smsService.DispatchSmsAsync(new SendSmsRequestDto
                {
                    PhoneNumber = rawPhone.Trim(),
                    Purpose = SmsPurposes.PaymentReceipt,
                    Amount = feePayment.Amount,
                    TransactionId = $"TXN-{paymentId}"
                });
            }

            await _billingRepo.SaveChangesAsync();
            return ServiceResult<FeePaymentResponseDto>.Success(MapToResponseDto(feePayment), 200);
        }

        /// <summary>
        /// GET: Compiles outstanding statements ledger summary charts [INDEX].
        /// </summary>
        public async Task<ServiceResult<IEnumerable<FeePaymentResponseDto>>> GetStudentLedgerAsync(int studentId)
        {
            var historyRecords = await _billingRepo.GetOutstandingFeesByStudentIdAsync(studentId);
            var mappedDtos = historyRecords.Select(MapToResponseDto);
            return ServiceResult<IEnumerable<FeePaymentResponseDto>>.Success(mappedDtos, 200);
        }

        public async Task<ServiceResult<IEnumerable<FeePaymentResponseDto>>> GetAllFeeAssignmentsAsync()
        {
            var historyRecords = await _billingRepo.GetAllFeeAssignmentsAsync();
            var mappedDtos = historyRecords.Select(MapToResponseDto);
            return ServiceResult<IEnumerable<FeePaymentResponseDto>>.Success(mappedDtos, 200);
        }

        private static FeePaymentResponseDto MapToResponseDto(FeePayment src)
        {
            return new FeePaymentResponseDto
            {
                Id = src.Id,
                StudentId = src.StudentId,
                FeeTypeName = src.FeeType?.Name ?? "General University Fee",
                Amount = src.Amount,
                BillingPeriod = src.BillingPeriod,
                Description = src.Description,
                Status = src.Status,
                PaidAt = src.PaidAt
            };
        }
    }
}