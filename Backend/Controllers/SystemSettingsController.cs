using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using Microsoft.EntityFrameworkCore;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Controllers
{
    [Authorize(Roles = "Admin,Student")]
    [ApiController]
    [Route("api/admin/system-settings")]
    public class SystemSettingsController : BaseApiController
    {
        private readonly AppDbContext _context;

        public SystemSettingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/admin/system-settings/reservation-hold-minutes - BRD Page 12
        [Authorize(Roles = "Admin,Student")]
        [HttpGet("reservation-hold-minutes")]
        public async Task<IActionResult> GetHoldMinutes()
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "LabBookingHoldMinutes" || s.SettingKey == "reservation-hold-minutes");

            int minutes = setting != null && int.TryParse(setting.SettingValue, out var val) ? val : 15;
            return ProcessServiceResult(ServiceResult<object>.Success(new { HoldMinutes = minutes }, 200), "Reservation hold timeout setting retrieved.");
        }

        // PUT /api/admin/system-settings/reservation-hold-minutes - BRD Page 12
        [HttpPut("reservation-hold-minutes")]
        public async Task<IActionResult> UpdateHoldMinutes([FromBody] UpdateHoldMinutesDto request)
        {
            if (request.HoldMinutes <= 0)
                return BadRequest("Reservation hold duration must be at least 1 minute.");

            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "LabBookingHoldMinutes");

            if (setting == null)
            {
                setting = new SystemSetting { SettingKey = "LabBookingHoldMinutes", SettingValue = request.HoldMinutes.ToString() };
                await _context.SystemSettings.AddAsync(setting);
            }
            else
            {
                setting.SettingValue = request.HoldMinutes.ToString();
                _context.SystemSettings.Update(setting);
            }

            await _context.SaveChangesAsync();
            return ProcessServiceResult(ServiceResult<object>.Success(new { Message = "Reservation hold timeout updated successfully.", HoldMinutes = request.HoldMinutes }, 200), "Reservation hold timeout setting updated.");
        }

        // GET /api/admin/system-settings/default-page-size
        [HttpGet("default-page-size")]
        public async Task<IActionResult> GetDefaultPageSize()
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "DefaultPageSize");

            int pageSize = setting != null && int.TryParse(setting.SettingValue, out var val) ? val : 5;
            return ProcessServiceResult(ServiceResult<object>.Success(new { PageSize = pageSize }, 200), "Default pagination page size setting retrieved.");
        }

        // PUT /api/admin/system-settings/default-page-size
        [HttpPut("default-page-size")]
        public async Task<IActionResult> UpdateDefaultPageSize([FromBody] UpdatePageSizeDto request)
        {
            if (request.PageSize <= 0)
                return BadRequest("Page size must be greater than zero.");

            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "DefaultPageSize");

            if (setting == null)
            {
                setting = new SystemSetting { SettingKey = "DefaultPageSize", SettingValue = request.PageSize.ToString() };
                await _context.SystemSettings.AddAsync(setting);
            }
            else
            {
                setting.SettingValue = request.PageSize.ToString();
                _context.SystemSettings.Update(setting);
            }

            await _context.SaveChangesAsync();
            return ProcessServiceResult(ServiceResult<object>.Success(new { Message = "System default page size updated successfully.", PageSize = request.PageSize }, 200), "Default pagination page size setting updated.");
        }

        // GET /api/admin/system-settings/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAllSettings()
        {
            try
            {
                var list = await _context.SystemSettings.ToListAsync();
                var dict = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

                foreach (var item in list)
                {
                    if (!string.IsNullOrWhiteSpace(item.SettingKey))
                    {
                        dict[item.SettingKey] = item.SettingValue ?? string.Empty;
                    }
                }

                // Standard Default Fallbacks & Auto-DB Seed if table entries are missing
                var defaultSeedMap = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["InstitutionName"] = "University of Knowledge (UOK)",
                    ["LabBookingHoldMinutes"] = dict.ContainsKey("reservation-hold-minutes") ? dict["reservation-hold-minutes"] : "15",
                    ["LabBookingSlotDurationMinutes"] = "15",
                    ["MaxLabBookingsPerStudentPerDay"] = "2",
                    ["MaxDailySlots"] = "2",
                    ["RequireSeatSelection"] = "true",
                    ["AcademicYear"] = "2025/2026",
                    ["AcademicYearsList"] = "2024/2025,2025/2026,2026/2027",
                    ["Semester"] = "Semester 1",
                    ["SemestersList"] = "Semester 1,Semester 2,Summer Trimester",
                    ["DefaultPageSize"] = "5",
                    // Finance & Fee Structure
                    ["LateFeeGracePeriodDays"] = "7",
                    ["DefaultCurrency"] = "LKR",
                    ["TaxPercentage"] = "0",
                    ["EnableOnlinePaymentGateway"] = "true",
                    // Student & Registration Types
                    ["StudentIdPrefixFormat"] = "STU/2026/",
                    ["MaxActiveRegistrationsPerStudent"] = "10",
                    ["RequireEmailVerificationOnRegistration"] = "true",
                    // Hostel & Room Allocation
                    ["HostelApplicationWindowDays"] = "30",
                    ["MaxRoomOccupancyCap"] = "4",
                    ["AutoApproveHostelApplications"] = "false",
                    // Campus Events & Venues
                    ["MaxAdvanceVenueBookingDays"] = "60",
                    ["RequireAdminApprovalForVenueBooking"] = "true",
                    ["EventRegistrationCancellationDeadlineHours"] = "24",
                    // Security & Authentication Policies
                    ["MinPasswordLength"] = "8",
                    ["MaxFailedLogins"] = "5",
                    ["AccountLockoutDurationMinutes"] = "15",
                    ["RequirePasswordComplexity"] = "strong",
                    ["PasswordExpiryDays"] = "90",
                    ["PasswordReuseHistoryLimit"] = "5",
                    ["OtpValidityMinutes"] = "3",
                    ["MaxOtpResendAttempts"] = "5",
                    ["TemporaryPasswordValidityHours"] = "24",
                    // Notifications & Templates
                    ["EnableEmailNotifications"] = "true",
                    ["EnableSmsNotifications"] = "false",
                    ["NotificationRetentionDays"] = "90"
                };

                bool addedNewSeed = false;
                foreach (var kvp in defaultSeedMap)
                {
                    if (!dict.ContainsKey(kvp.Key))
                    {
                        dict[kvp.Key] = kvp.Value;
                        await _context.SystemSettings.AddAsync(new SystemSetting { SettingKey = kvp.Key, SettingValue = kvp.Value });
                        addedNewSeed = true;
                    }
                }

                if (addedNewSeed)
                {
                    await _context.SaveChangesAsync();
                }

                return ProcessServiceResult(ServiceResult<object>.Success(dict, 200), "All system settings retrieved successfully.");
            }
            catch (System.Exception ex)
            {
                var fallback = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["InstitutionName"] = "University of Knowledge (UOK)",
                    ["LabBookingHoldMinutes"] = "15",
                    ["reservation-hold-minutes"] = "15",
                    ["MaxDailySlots"] = "2",
                    ["RequireSeatSelection"] = "true",
                    ["AcademicYear"] = "2025/2026",
                    ["AcademicYearsList"] = "2024/2025,2025/2026,2026/2027",
                    ["Semester"] = "Semester 1",
                    ["SemestersList"] = "Semester 1,Semester 2,Summer Trimester",
                    ["DefaultPageSize"] = "5"
                };
                return ProcessServiceResult(ServiceResult<object>.Success(fallback, 200), "Default system settings fallback loaded.");
            }
        }

        // PUT /api/admin/system-settings/batch
        [HttpPut("batch")]
        public async Task<IActionResult> UpdateSettingsBatch([FromBody] System.Collections.Generic.Dictionary<string, string> settingsPayload)
        {
            if (settingsPayload == null || settingsPayload.Count == 0)
                return BadRequest("Settings payload cannot be empty.");

            foreach (var kvp in settingsPayload)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key)) continue;

                var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == kvp.Key);
                if (setting == null)
                {
                    await _context.SystemSettings.AddAsync(new SystemSetting { SettingKey = kvp.Key, SettingValue = kvp.Value ?? string.Empty });
                }
                else
                {
                    setting.SettingValue = kvp.Value ?? string.Empty;
                    _context.SystemSettings.Update(setting);
                }
            }

            await _context.SaveChangesAsync();
            return await GetAllSettings();
        }
    }

    public record UpdateHoldMinutesDto(int HoldMinutes);
    public record UpdatePageSizeDto(int PageSize);
}
