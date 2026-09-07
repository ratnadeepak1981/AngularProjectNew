using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CampusServicesPortal.Data;
using CampusServicesPortal.DTOs.Requests.Sms;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CampusServicesPortal.Services.Implementations
{
    public class SmsService : ISmsService
    {
        private readonly IPasswordRepository _passwordRepo;
        private readonly ILogger<SmsService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _memoryCache;
        private readonly AppDbContext _context;

        public SmsService(
            IPasswordRepository passwordRepo, 
            ILogger<SmsService> logger, 
            IWebHostEnvironment env,
            IMemoryCache memoryCache,
            AppDbContext context)
        {
            _passwordRepo = passwordRepo;
            _logger = logger;
            _env = env;
            _memoryCache = memoryCache;
            _context = context;
        }

        private static string NormalizePhoneKey(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : Regex.Replace(raw, @"[^\d]", "");
        }

        private async Task<string> ResolveStudentNameAsync(string emailOrPhone)
        {
            if (string.IsNullOrWhiteSpace(emailOrPhone)) return "Student User";
            string cleanInput = emailOrPhone.Trim();
            string digitsOnly = NormalizePhoneKey(cleanInput);

            try
            {
                // 1. Check by Email
                var studentByEmail = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.User.Email.ToLower() == cleanInput.ToLower());
                if (studentByEmail != null && !string.IsNullOrWhiteSpace(studentByEmail.FullName))
                    return studentByEmail.FullName;

                // 2. Check by Index Number
                var studentByIndex = await _context.Students
                    .FirstOrDefaultAsync(s => s.IndexNumber.ToLower() == cleanInput.ToLower());
                if (studentByIndex != null && !string.IsNullOrWhiteSpace(studentByIndex.FullName))
                    return studentByIndex.FullName;

                // 3. Check by Phone Number
                if (!string.IsNullOrEmpty(digitsOnly) && digitsOnly.Length >= 6)
                {
                    var phoneRecord = await _context.StudentPhoneNumbers
                        .Include(p => p.Student)
                        .FirstOrDefaultAsync(p => p.PhoneNumber.Contains(digitsOnly) || digitsOnly.Contains(p.PhoneNumber.Replace(" ", "").Replace("-", "").Replace("+", "")));
                    if (phoneRecord?.Student != null && !string.IsNullOrWhiteSpace(phoneRecord.Student.FullName))
                        return phoneRecord.Student.FullName;

                    var studentByContact = await _context.Students
                        .FirstOrDefaultAsync(s => s.ContactDetails != null && s.ContactDetails.Contains(digitsOnly));
                    if (studentByContact != null && !string.IsNullOrWhiteSpace(studentByContact.FullName))
                        return studentByContact.FullName;
                }

                // 4. Check by Master List
                var master = await _context.StudentMasterLists
                    .FirstOrDefaultAsync(m => m.IndexNumber.ToLower() == cleanInput.ToLower());
                if (master != null && !string.IsNullOrWhiteSpace(master.FullName))
                    return master.FullName;
            }
            catch
            {
                // Fallback
            }

            return "Student User";
        }

        public Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("=========================================================================================");
            _logger.LogInformation("[SMS SIMULATION GATEWAY] 📱");
            _logger.LogInformation("To          : {PhoneNumber}", phoneNumber);
            _logger.LogInformation("Timestamp   : {Timestamp} UTC", DateTime.UtcNow.ToString("g"));
            _logger.LogInformation("Message Body: {Message}", message);
            _logger.LogInformation("=========================================================================================");

            return Task.FromResult(true);
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
            catch
            {
                // Fallback
            }
            return 3;
        }

        public async Task<ServiceResult<object>> DispatchSmsAsync(SendSmsRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return ServiceResult<object>.Failure("Phone number is required.", 400);
            }

            int validityMinutes = await GetOtpValidityMinutesAsync();

            string otp = string.IsNullOrWhiteSpace(request.OtpCode) 
                ? RandomNumberGenerator.GetInt32(100000, 1000000).ToString() 
                : request.OtpCode;

            string msg = request.MessageOverride ?? string.Empty;
            if (string.IsNullOrWhiteSpace(msg))
            {
                string cleanPurpose = request.Purpose ?? string.Empty;
                if (cleanPurpose.Equals(SmsPurposes.ForgotPasswordOtp, StringComparison.OrdinalIgnoreCase) || cleanPurpose == "0")
                {
                    msg = $"Campus Services Portal: Your password reset OTP is {otp}. Valid for {validityMinutes} minutes. Do not share this OTP with anyone.";
                }
                else if (cleanPurpose.Equals(SmsPurposes.RegistrationOtp, StringComparison.OrdinalIgnoreCase) || cleanPurpose == "1")
                {
                    msg = $"Campus Services Portal: Your Student Registration mobile verification OTP is {otp}. Valid for {validityMinutes} minutes. Do NOT share.";
                    string cleanPhone = NormalizePhoneKey(request.PhoneNumber);
                    if (!string.IsNullOrEmpty(cleanPhone))
                    {
                        _memoryCache.Set($"PhoneOtp_Phone_{cleanPhone}", otp, TimeSpan.FromMinutes(validityMinutes));
                    }
                    _memoryCache.Set("LatestPhoneOtp", otp, TimeSpan.FromMinutes(validityMinutes));
                    _memoryCache.Set("LatestPhoneNumber", request.PhoneNumber, TimeSpan.FromMinutes(validityMinutes));
                }
                else if (cleanPurpose.Equals(SmsPurposes.PrimaryMobileUpdateOtp, StringComparison.OrdinalIgnoreCase))
                {
                    msg = $"Campus Services Portal: Your Primary Mobile change verification OTP is {otp}. Valid for {validityMinutes} minutes. Do NOT share.";
                    string cleanPhone = NormalizePhoneKey(request.PhoneNumber);
                    if (!string.IsNullOrEmpty(cleanPhone))
                    {
                        _memoryCache.Set($"PhoneOtp_Phone_{cleanPhone}", otp, TimeSpan.FromMinutes(validityMinutes));
                    }
                    _memoryCache.Set("LatestPhoneOtp", otp, TimeSpan.FromMinutes(validityMinutes));
                    _memoryCache.Set("LatestPhoneNumber", request.PhoneNumber, TimeSpan.FromMinutes(validityMinutes));
                }
                else if (cleanPurpose.Equals(SmsPurposes.PaymentOtp, StringComparison.OrdinalIgnoreCase) || cleanPurpose == "2")
                {
                    decimal amt = request.Amount ?? 5000.00m;
                    string txn = !string.IsNullOrWhiteSpace(request.TransactionId) ? request.TransactionId : "TXN-849201";
                    msg = $"Campus Payment Gateway: OTP {otp} to authorize LKR {amt:N2} for Tuition Settlement (Ref: {txn}). Valid {validityMinutes} mins. Do NOT share.";

                    string cleanPaymentPhone = NormalizePhoneKey(request.PhoneNumber);
                    if (!string.IsNullOrEmpty(cleanPaymentPhone))
                    {
                        _memoryCache.Set($"PaymentOtp_Phone_{cleanPaymentPhone}", otp, TimeSpan.FromMinutes(validityMinutes));
                        _memoryCache.Set($"PaymentAmount_Phone_{cleanPaymentPhone}", amt, TimeSpan.FromMinutes(validityMinutes));
                        _memoryCache.Set($"PaymentTxn_Phone_{cleanPaymentPhone}", txn, TimeSpan.FromMinutes(validityMinutes));
                    }
                }
                else if (cleanPurpose.Equals(SmsPurposes.PaymentReceipt, StringComparison.OrdinalIgnoreCase) || cleanPurpose == "3")
                {
                    decimal receiptAmt = request.Amount ?? 5000.00m;
                    msg = $"Campus Finance: Payment CLEARED! LKR {receiptAmt:N2} for Semester Fees processed (Ref: {request.TransactionId ?? "TXN-849201"}). Thank you.";
                }
                else
                {
                    msg = $"Campus Alert: {otp}";
                }
            }

            await SendSmsAsync(request.PhoneNumber, msg);
            return ServiceResult<object>.Success(new { Message = "SMS dispatched successfully to simulation gateway.", To = request.PhoneNumber }, 200);
        }

        public async Task<ServiceResult<string>> GenerateForgotPasswordSmsPreviewAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return ServiceResult<string>.Failure("A valid registered email address is required.", 400);
            }

            string cleanEmail = email.Trim().ToLowerInvariant();
            var student = await _passwordRepo.GetStudentByEmailThroughUserAsync(cleanEmail);
            var user = await _passwordRepo.GetUserByEmailAsync(cleanEmail);

            if (user == null && student == null)
            {
                return ServiceResult<string>.Failure($"No registered user account found for email '{cleanEmail}'.", 404);
            }

            string tokenCode = string.Empty;
            int remainingMins = 3;

            if (_memoryCache.TryGetValue($"PasswordResetOtp_{cleanEmail}", out string? cachedOtp) && !string.IsNullOrEmpty(cachedOtp))
            {
                tokenCode = cachedOtp;
                int validityMinutes = await GetOtpValidityMinutesAsync();
                remainingMins = validityMinutes;
            }
            else if (student != null)
            {
                var latestToken = await _context.PasswordResetTokens
                    .Where(p => p.StudentId == student.Id && !p.IsUsed && p.ExpiresAt >= DateTime.UtcNow)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync();

                if (latestToken != null)
                {
                    tokenCode = latestToken.Token;
                    remainingMins = Math.Max(1, (int)Math.Ceiling((latestToken.ExpiresAt - DateTime.UtcNow).TotalMinutes));
                }
            }

            if (string.IsNullOrEmpty(tokenCode))
            {
                return ServiceResult<string>.Failure($"No active password reset OTP session found for '{cleanEmail}'. Please initiate a reset request first.", 404);
            }

            string fullName = student?.FullName ?? await ResolveStudentNameAsync(cleanEmail);
            string expiresAtStr = $"Valid for {remainingMins} minutes (Expires in ~{remainingMins} mins)";
            string phoneNo = student?.PhoneNumbers?.FirstOrDefault(p => p.IsPrimary)?.PhoneNumber 
                             ?? (!string.IsNullOrWhiteSpace(student?.ContactDetails) ? student.ContactDetails : "Mobile line on record");

            string templatePath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Sms", "ForgotPasswordOtp.cshtml");
            string cssPath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Sms", "ForgotPasswordOtp.css");

            if (!File.Exists(templatePath) || !File.Exists(cssPath))
            {
                return ServiceResult<string>.Failure("SMS OTP preview template files missing on server.", 500);
            }

            string cssContent = await File.ReadAllTextAsync(cssPath);
            string htmlTemplate = await File.ReadAllTextAsync(templatePath);

            string currentTimeShort = DateTime.Now.ToString("h:mm tt");
            string currentDateTime = DateTime.Now.ToString("MMM dd, yyyy • h:mm tt");

            string renderedHtml = htmlTemplate
                .Replace("{{CSS_CONTENT}}", cssContent)
                .Replace("{{FULL_NAME}}", fullName)
                .Replace("{{PHONE_NUMBER}}", phoneNo)
                .Replace("{{TOKEN_CODE}}", tokenCode)
                .Replace("{{EXPIRES_AT_STR}}", expiresAtStr)
                .Replace("{{SUB_HEADER}}", "Password Reset Token Dispatch")
                .Replace("{{PURPOSE_DESC}}", "A password reset request was initiated for your Campus Portal account.")
                .Replace("{{CURRENT_TIME_SHORT}}", currentTimeShort)
                .Replace("{{CURRENT_DATE_TIME}}", currentDateTime);

            return ServiceResult<string>.Success(renderedHtml, 200);
        }

        public async Task<ServiceResult<string>> GeneratePaymentOtpSmsPreviewAsync(string? phoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return ServiceResult<string>.Failure("Phone number query parameter is required to preview active Payment OTP.", 400);
            }

            string cleanPhone = NormalizePhoneKey(phoneNumber);
            if (string.IsNullOrEmpty(cleanPhone) || cleanPhone.Length < 7)
            {
                return ServiceResult<string>.Failure("Invalid phone number provided.", 400);
            }

            string tokenCode = string.Empty;
            decimal finalAmt = 5000.00m;
            string txnId = "TXN-849201";

            if (_memoryCache.TryGetValue($"PaymentOtp_Phone_{cleanPhone}", out string? cachedPhoneOtp) && !string.IsNullOrEmpty(cachedPhoneOtp))
            {
                tokenCode = cachedPhoneOtp;
                if (_memoryCache.TryGetValue($"PaymentAmount_Phone_{cleanPhone}", out decimal cachedAmt)) finalAmt = cachedAmt;
                if (_memoryCache.TryGetValue($"PaymentTxn_Phone_{cleanPhone}", out string? cachedTxn) && !string.IsNullOrEmpty(cachedTxn)) txnId = cachedTxn;
            }

            if (string.IsNullOrEmpty(tokenCode))
            {
                return ServiceResult<string>.Failure($"No active Payment OTP session found for phone number '{phoneNumber.Trim()}'. Please initiate payment verification first.", 404);
            }

            string phoneNo = phoneNumber.Trim();
            string fullName = await ResolveStudentNameAsync(phoneNo);
            int validityMinutes = await GetOtpValidityMinutesAsync();

            string templatePath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Sms", "PaymentOtp.cshtml");
            string cssPath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Sms", "PaymentOtp.css");

            if (!File.Exists(templatePath) || !File.Exists(cssPath))
            {
                return ServiceResult<string>.Failure("Payment OTP template files missing on server.", 500);
            }

            string cssContent = await File.ReadAllTextAsync(cssPath);
            string htmlTemplate = await File.ReadAllTextAsync(templatePath);

            string currentTimeShort = DateTime.Now.ToString("h:mm tt");
            string currentDateTime = DateTime.Now.ToString("MMM dd, yyyy • h:mm tt");

            string renderedHtml = htmlTemplate
                .Replace("{{CSS_CONTENT}}", cssContent)
                .Replace("{{FULL_NAME}}", fullName)
                .Replace("{{PHONE_NUMBER}}", phoneNo)
                .Replace("{{TOKEN_CODE}}", tokenCode)
                .Replace("{{AMOUNT}}", finalAmt.ToString("N2"))
                .Replace("{{TRANSACTION_ID}}", txnId)
                .Replace("{{EXPIRES_AT_STR}}", $"Valid for {validityMinutes} minutes (Expires in ~{validityMinutes} mins)")
                .Replace("{{CURRENT_TIME_SHORT}}", currentTimeShort)
                .Replace("{{CURRENT_DATE_TIME}}", currentDateTime);

            return ServiceResult<string>.Success(renderedHtml, 200);
        }

        public async Task<ServiceResult<string>> GeneratePaymentReceiptSmsPreviewAsync(string email, decimal? amount = null, string? transactionId = null)
        {
            string cleanEmail = string.IsNullOrWhiteSpace(email) ? "ruwanbandara@univercity.co.lk" : email.Trim();
            var student = await _passwordRepo.GetStudentByEmailThroughUserAsync(cleanEmail);

            string fullName = student?.FullName ?? "Ruwan Bandara";
            string phoneNo = student?.ContactDetails ?? "+94 77 123 4567";
            decimal finalAmt = amount ?? 5000.00m;
            string txnId = string.IsNullOrWhiteSpace(transactionId) ? "TXN-" + new Random().Next(100000, 999999) : transactionId;

            string templatePath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Sms", "PaymentReceipt.cshtml");
            string cssPath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Sms", "PaymentReceipt.css");

            if (!File.Exists(templatePath) || !File.Exists(cssPath))
            {
                return ServiceResult<string>.Failure("Payment receipt template files missing on server.", 500);
            }

            string cssContent = await File.ReadAllTextAsync(cssPath);
            string htmlTemplate = await File.ReadAllTextAsync(templatePath);

            string currentTimeShort = DateTime.Now.ToString("h:mm tt");
            string currentDateTime = DateTime.Now.ToString("MMM dd, yyyy • h:mm tt");

            string renderedHtml = htmlTemplate
                .Replace("{{CSS_CONTENT}}", cssContent)
                .Replace("{{FULL_NAME}}", fullName)
                .Replace("{{PHONE_NUMBER}}", phoneNo)
                .Replace("{{AMOUNT}}", finalAmt.ToString("N2"))
                .Replace("{{TRANSACTION_ID}}", txnId)
                .Replace("{{CURRENT_TIME_SHORT}}", currentTimeShort)
                .Replace("{{CURRENT_DATE_TIME}}", currentDateTime);

            return ServiceResult<string>.Success(renderedHtml, 200);
        }

        public async Task<ServiceResult<string>> GeneratePhoneOtpSmsPreviewAsync(string? phoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return ServiceResult<string>.Failure("Phone number query parameter is required to preview Phone OTP.", 400);
            }

            string cleanPhone = NormalizePhoneKey(phoneNumber);
            if (string.IsNullOrEmpty(cleanPhone) || cleanPhone.Length < 7)
            {
                return ServiceResult<string>.Failure("Invalid phone number provided.", 400);
            }

            string code = string.Empty;
            if (_memoryCache.TryGetValue($"PhoneOtp_Phone_{cleanPhone}", out string? cachedPhoneOtp) && !string.IsNullOrEmpty(cachedPhoneOtp))
            {
                code = cachedPhoneOtp;
            }

            if (string.IsNullOrEmpty(code))
            {
                return ServiceResult<string>.Failure($"No active mobile OTP session found for phone number '{phoneNumber.Trim()}'. Please request a phone OTP first.", 404);
            }

            string phoneNo = phoneNumber.Trim();
            string fullName = await ResolveStudentNameAsync(phoneNo);
            int validityMinutes = await GetOtpValidityMinutesAsync();

            string templatePath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Sms", "ForgotPasswordOtp.cshtml");
            string cssPath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Sms", "ForgotPasswordOtp.css");

            if (!File.Exists(templatePath) || !File.Exists(cssPath))
            {
                return ServiceResult<string>.Failure("SMS preview template files missing on server.", 500);
            }

            string cssContent = await File.ReadAllTextAsync(cssPath);
            string htmlTemplate = await File.ReadAllTextAsync(templatePath);

            string currentTimeShort = DateTime.Now.ToString("h:mm tt");
            string currentDateTime = DateTime.Now.ToString("MMM dd, yyyy • h:mm tt");

            string renderedHtml = htmlTemplate
                .Replace("{{CSS_CONTENT}}", cssContent)
                .Replace("{{FULL_NAME}}", fullName)
                .Replace("{{PHONE_NUMBER}}", phoneNo)
                .Replace("{{TOKEN_CODE}}", code)
                .Replace("{{EXPIRES_AT_STR}}", $"Valid for {validityMinutes} minutes (Expires in ~{validityMinutes} mins)")
                .Replace("{{SUB_HEADER}}", "Mobile Phone OTP Verification")
                .Replace("{{PURPOSE_DESC}}", "Your mobile verification security OTP code for the Campus Services Portal is:")
                .Replace("{{CURRENT_TIME_SHORT}}", currentTimeShort)
                .Replace("{{CURRENT_DATE_TIME}}", currentDateTime);

            return ServiceResult<string>.Success(renderedHtml, 200);
        }
    }
}
