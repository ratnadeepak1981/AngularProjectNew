using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Reports;
using CampusServicesPortal.DTOs.Responses.Reports;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CampusServicesPortal.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<ReportService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportService(
            IReportRepository reportRepository,
            IAuditLogService auditLogService,
            ILogger<ReportService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _reportRepository = reportRepository;
            _auditLogService = auditLogService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private (int? userId, string displayName) GetCurrentUserInfo()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return (null, "System Administrator");

            int? userId = null;
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            if (int.TryParse(idClaim, out var parsedId))
            {
                userId = parsedId;
            }

            var displayName = user.FindFirst(ClaimTypes.Email)?.Value 
                ?? user.FindFirst(ClaimTypes.Name)?.Value 
                ?? user.Identity?.Name 
                ?? "System Administrator";

            return (userId, displayName);
        }

        public async Task<ServiceResult<InstitutionalKpiReportDto>> GetInstitutionalKpiSummaryAsync()
        {
            try
            {
                var summary = await _reportRepository.GetInstitutionalKpiSummaryAsync();
                var (userId, displayName) = GetCurrentUserInfo();

                await _auditLogService.LogActivityAsync(
                    userId,
                    displayName,
                    "ViewKpiReport",
                    "Reports",
                    null,
                    "Administrator generated Institutional KPI Overview report.",
                    true);

                return ServiceResult<InstitutionalKpiReportDto>.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Institutional KPI Summary.");
                return ServiceResult<InstitutionalKpiReportDto>.Failure("Failed to retrieve institutional KPI metrics.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<StudentDetailItemDto>>> GetStudentReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetStudentReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();

                var desc = filter.DrilldownId.HasValue 
                    ? $"Drilldown subreport accessed for Faculty #{filter.DrilldownId.Value}." 
                    : "Student Demographics report generated.";

                await _auditLogService.LogActivityAsync(
                    userId,
                    displayName,
                    filter.DrilldownId.HasValue ? "DrilldownStudentReport" : "ViewStudentReport",
                    "Reports",
                    filter.DrilldownId?.ToString(),
                    desc,
                    true);

                return ServiceResult<PagedReportResultDto<StudentDetailItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Student Report.");
                return ServiceResult<PagedReportResultDto<StudentDetailItemDto>>.Failure("Failed to generate student report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<HostelDetailItemDto>>> GetHostelReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetHostelReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();

                var desc = filter.DrilldownId.HasValue 
                    ? $"Drilldown subreport accessed for Hostel #{filter.DrilldownId.Value}." 
                    : "Hostel Occupancy report generated.";

                await _auditLogService.LogActivityAsync(
                    userId,
                    displayName,
                    filter.DrilldownId.HasValue ? "DrilldownHostelReport" : "ViewHostelReport",
                    "Reports",
                    filter.DrilldownId?.ToString(),
                    desc,
                    true);

                return ServiceResult<PagedReportResultDto<HostelDetailItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Hostel Report.");
                return ServiceResult<PagedReportResultDto<HostelDetailItemDto>>.Failure("Failed to generate hostel accommodation report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<LabBookingDetailItemDto>>> GetLabReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetLabReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();

                var desc = filter.DrilldownId.HasValue 
                    ? $"Drilldown subreport accessed for Lab #{filter.DrilldownId.Value}." 
                    : "Lab Utilization report generated.";

                await _auditLogService.LogActivityAsync(
                    userId,
                    displayName,
                    filter.DrilldownId.HasValue ? "DrilldownLabReport" : "ViewLabReport",
                    "Reports",
                    filter.DrilldownId?.ToString(),
                    desc,
                    true);

                return ServiceResult<PagedReportResultDto<LabBookingDetailItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Lab Report.");
                return ServiceResult<PagedReportResultDto<LabBookingDetailItemDto>>.Failure("Failed to generate lab utilization report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<PaymentDetailItemDto>>> GetBillingReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetBillingReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();

                var desc = filter.DrilldownId.HasValue 
                    ? $"Drilldown subreport accessed for Fee Type #{filter.DrilldownId.Value}." 
                    : "Billing & Financial Ledger report generated.";

                await _auditLogService.LogActivityAsync(
                    userId,
                    displayName,
                    filter.DrilldownId.HasValue ? "DrilldownBillingReport" : "ViewBillingReport",
                    "Reports",
                    filter.DrilldownId?.ToString(),
                    desc,
                    true);

                return ServiceResult<PagedReportResultDto<PaymentDetailItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Billing Report.");
                return ServiceResult<PagedReportResultDto<PaymentDetailItemDto>>.Failure("Failed to generate billing and fees report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<ComplaintDetailItemDto>>> GetComplaintReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetComplaintReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();

                var desc = filter.DrilldownId.HasValue 
                    ? $"Drilldown subreport accessed for Complaint Category #{filter.DrilldownId.Value}." 
                    : "Complaint & Grievance Triage report generated.";

                await _auditLogService.LogActivityAsync(
                    userId,
                    displayName,
                    filter.DrilldownId.HasValue ? "DrilldownComplaintReport" : "ViewComplaintReport",
                    "Reports",
                    filter.DrilldownId?.ToString(),
                    desc,
                    true);

                return ServiceResult<PagedReportResultDto<ComplaintDetailItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Complaint Report.");
                return ServiceResult<PagedReportResultDto<ComplaintDetailItemDto>>.Failure("Failed to generate grievance complaints report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<CertificateRequestDetailItemDto>>> GetCertificateReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetCertificateReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();

                var desc = filter.DrilldownId.HasValue 
                    ? $"Drilldown subreport accessed for Certificate Type #{filter.DrilldownId.Value}." 
                    : "Certificate Issuance report generated.";

                await _auditLogService.LogActivityAsync(
                    userId,
                    displayName,
                    filter.DrilldownId.HasValue ? "DrilldownCertificateReport" : "ViewCertificateReport",
                    "Reports",
                    filter.DrilldownId?.ToString(),
                    desc,
                    true);

                return ServiceResult<PagedReportResultDto<CertificateRequestDetailItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Certificate Report.");
                return ServiceResult<PagedReportResultDto<CertificateRequestDetailItemDto>>.Failure("Failed to generate certificate request report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<EventRegistrationDetailItemDto>>> GetEventReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetEventReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();

                var desc = filter.DrilldownId.HasValue 
                    ? $"Drilldown subreport accessed for Event #{filter.DrilldownId.Value}." 
                    : "Event Participation report generated.";

                await _auditLogService.LogActivityAsync(
                    userId,
                    displayName,
                    filter.DrilldownId.HasValue ? "DrilldownEventReport" : "ViewEventReport",
                    "Reports",
                    filter.DrilldownId?.ToString(),
                    desc,
                    true);

                return ServiceResult<PagedReportResultDto<EventRegistrationDetailItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Event Report.");
                return ServiceResult<PagedReportResultDto<EventRegistrationDetailItemDto>>.Failure("Failed to generate event participation report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<NotificationDetailItemDto>>> GetNotificationReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetNotificationReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();

                var desc = !string.IsNullOrWhiteSpace(filter.DrilldownKey) 
                    ? $"Drilldown subreport accessed for Notification Type '{filter.DrilldownKey}'." 
                    : "System Notification Dispatch report generated.";

                await _auditLogService.LogActivityAsync(
                    userId,
                    displayName,
                    !string.IsNullOrWhiteSpace(filter.DrilldownKey) ? "DrilldownNotificationReport" : "ViewNotificationReport",
                    "Reports",
                    filter.DrilldownKey,
                    desc,
                    true);

                return ServiceResult<PagedReportResultDto<NotificationDetailItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Notification Report.");
                return ServiceResult<PagedReportResultDto<NotificationDetailItemDto>>.Failure("Failed to generate notification analytics report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<HostelRoomDetailItemDto>>> GetHostelRoomsReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetHostelRoomsReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();
                await _auditLogService.LogActivityAsync(userId, displayName, "ViewHostelRoomsReport", "Reports", null, "Hostel Rooms and Inventory report generated.", true);
                return ServiceResult<PagedReportResultDto<HostelRoomDetailItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Hostel Rooms Report.");
                return ServiceResult<PagedReportResultDto<HostelRoomDetailItemDto>>.Failure("Failed to generate hostel rooms inventory report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<PendingHostelApplicationItemDto>>> GetPendingHostelApplicationsReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetPendingHostelApplicationsReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();
                await _auditLogService.LogActivityAsync(userId, displayName, "ViewPendingHostelApplicationsReport", "Reports", null, "Pending Hostel Applications Queue report generated.", true);
                return ServiceResult<PagedReportResultDto<PendingHostelApplicationItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Pending Hostel Applications Report.");
                return ServiceResult<PagedReportResultDto<PendingHostelApplicationItemDto>>.Failure("Failed to generate pending hostel applications report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<LabDirectoryItemDto>>> GetLabDirectoryReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetLabDirectoryReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();
                await _auditLogService.LogActivityAsync(userId, displayName, "ViewLabDirectoryReport", "Reports", null, "Lab Directory & Layout Configuration report generated.", true);
                return ServiceResult<PagedReportResultDto<LabDirectoryItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Lab Directory Report.");
                return ServiceResult<PagedReportResultDto<LabDirectoryItemDto>>.Failure("Failed to generate lab directory report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<VenueUtilizationItemDto>>> GetVenueUtilizationReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetVenueUtilizationReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();
                await _auditLogService.LogActivityAsync(userId, displayName, "ViewVenueUtilizationReport", "Reports", null, "Campus Venues & Facility Utilization report generated.", true);
                return ServiceResult<PagedReportResultDto<VenueUtilizationItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Venue Utilization Report.");
                return ServiceResult<PagedReportResultDto<VenueUtilizationItemDto>>.Failure("Failed to generate venue utilization report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<PendingStudentRegistrationItemDto>>> GetPendingStudentRegistrationsReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetPendingStudentRegistrationsReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();
                await _auditLogService.LogActivityAsync(userId, displayName, "ViewPendingStudentRegistrationsReport", "Reports", null, "Pending Student Registrations report generated.", true);
                return ServiceResult<PagedReportResultDto<PendingStudentRegistrationItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Pending Student Registrations Report.");
                return ServiceResult<PagedReportResultDto<PendingStudentRegistrationItemDto>>.Failure("Failed to generate pending registrations report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<CertificateTypeCatalogItemDto>>> GetCertificateTypesCatalogReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetCertificateTypesCatalogReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();
                await _auditLogService.LogActivityAsync(userId, displayName, "ViewCertificateTypesCatalogReport", "Reports", null, "Certificate Types and Service Catalog report generated.", true);
                return ServiceResult<PagedReportResultDto<CertificateTypeCatalogItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Certificate Types Catalog Report.");
                return ServiceResult<PagedReportResultDto<CertificateTypeCatalogItemDto>>.Failure("Failed to generate certificate types catalog report.");
            }
        }

        public async Task<ServiceResult<PagedReportResultDto<ComplaintCategorySlaItemDto>>> GetComplaintCategoriesSlaReportAsync(ReportFilterDto filter)
        {
            try
            {
                var report = await _reportRepository.GetComplaintCategoriesSlaReportAsync(filter);
                var (userId, displayName) = GetCurrentUserInfo();
                await _auditLogService.LogActivityAsync(userId, displayName, "ViewComplaintCategoriesSlaReport", "Reports", null, "Complaint Categories & SLA Performance report generated.", true);
                return ServiceResult<PagedReportResultDto<ComplaintCategorySlaItemDto>>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Complaint Categories SLA Report.");
                return ServiceResult<PagedReportResultDto<ComplaintCategorySlaItemDto>>.Failure("Failed to generate complaint categories SLA report.");
            }
        }
    }
}
