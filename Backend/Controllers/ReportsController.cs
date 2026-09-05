using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Reports;
using CampusServicesPortal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusServicesPortal.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/reports")]
    public class ReportsController : BaseApiController
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: /api/admin/reports/kpi-summary
        [HttpGet("kpi-summary")]
        public async Task<IActionResult> GetInstitutionalKpiSummary()
        {
            var result = await _reportService.GetInstitutionalKpiSummaryAsync();
            return ProcessServiceResult(result, "Institutional KPI summary retrieved successfully.");
        }

        // GET: /api/admin/reports/students
        [HttpGet("students")]
        public async Task<IActionResult> GetStudentReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetStudentReportAsync(filter);
            return ProcessServiceResult(result, "Student enrollment and demographic report retrieved successfully.");
        }

        // GET: /api/admin/reports/hostels
        [HttpGet("hostels")]
        public async Task<IActionResult> GetHostelReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetHostelReportAsync(filter);
            return ProcessServiceResult(result, "Hostel occupancy report retrieved successfully.");
        }

        // GET: /api/admin/reports/labs
        [HttpGet("labs")]
        public async Task<IActionResult> GetLabReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetLabReportAsync(filter);
            return ProcessServiceResult(result, "Lab utilization and booking report retrieved successfully.");
        }

        // GET: /api/admin/reports/billing
        [HttpGet("billing")]
        public async Task<IActionResult> GetBillingReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetBillingReportAsync(filter);
            return ProcessServiceResult(result, "Billing and fee collection report retrieved successfully.");
        }

        // GET: /api/admin/reports/complaints
        [HttpGet("complaints")]
        public async Task<IActionResult> GetComplaintReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetComplaintReportAsync(filter);
            return ProcessServiceResult(result, "Grievance and complaint triage report retrieved successfully.");
        }

        // GET: /api/admin/reports/certificates
        [HttpGet("certificates")]
        public async Task<IActionResult> GetCertificateReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetCertificateReportAsync(filter);
            return ProcessServiceResult(result, "Certificate issuance and request report retrieved successfully.");
        }

        // GET: /api/admin/reports/events
        [HttpGet("events")]
        public async Task<IActionResult> GetEventReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetEventReportAsync(filter);
            return ProcessServiceResult(result, "Event participation report retrieved successfully.");
        }

        // GET: /api/admin/reports/notifications
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotificationReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetNotificationReportAsync(filter);
            return ProcessServiceResult(result, "System notification analytics report retrieved successfully.");
        }

        [HttpGet("hostel-rooms")]
        public async Task<IActionResult> GetHostelRoomsReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetHostelRoomsReportAsync(filter);
            return ProcessServiceResult(result, "Hostel rooms inventory report retrieved successfully.");
        }

        [HttpGet("hostel-pending-applications")]
        public async Task<IActionResult> GetPendingHostelApplicationsReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetPendingHostelApplicationsReportAsync(filter);
            return ProcessServiceResult(result, "Pending hostel applications report retrieved successfully.");
        }

        [HttpGet("lab-directory")]
        public async Task<IActionResult> GetLabDirectoryReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetLabDirectoryReportAsync(filter);
            return ProcessServiceResult(result, "Lab directory and layout report retrieved successfully.");
        }

        [HttpGet("venues")]
        public async Task<IActionResult> GetVenueUtilizationReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetVenueUtilizationReportAsync(filter);
            return ProcessServiceResult(result, "Event venues and facility report retrieved successfully.");
        }

        [HttpGet("student-pending-registrations")]
        public async Task<IActionResult> GetPendingStudentRegistrationsReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetPendingStudentRegistrationsReportAsync(filter);
            return ProcessServiceResult(result, "Pending student registrations report retrieved successfully.");
        }

        [HttpGet("certificate-types")]
        public async Task<IActionResult> GetCertificateTypesCatalogReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetCertificateTypesCatalogReportAsync(filter);
            return ProcessServiceResult(result, "Certificate types and service catalog report retrieved successfully.");
        }

        [HttpGet("complaint-categories-sla")]
        public async Task<IActionResult> GetComplaintCategoriesSlaReport([FromQuery] ReportFilterDto filter)
        {
            var result = await _reportService.GetComplaintCategoriesSlaReportAsync(filter);
            return ProcessServiceResult(result, "Complaint categories SLA report retrieved successfully.");
        }
    }
}
