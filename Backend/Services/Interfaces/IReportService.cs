using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Reports;
using CampusServicesPortal.DTOs.Responses.Reports;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Interfaces
{
    public interface IReportService
    {
        Task<ServiceResult<InstitutionalKpiReportDto>> GetInstitutionalKpiSummaryAsync();
        Task<ServiceResult<PagedReportResultDto<StudentDetailItemDto>>> GetStudentReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<HostelDetailItemDto>>> GetHostelReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<LabBookingDetailItemDto>>> GetLabReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<PaymentDetailItemDto>>> GetBillingReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<ComplaintDetailItemDto>>> GetComplaintReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<CertificateRequestDetailItemDto>>> GetCertificateReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<EventRegistrationDetailItemDto>>> GetEventReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<NotificationDetailItemDto>>> GetNotificationReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<HostelRoomDetailItemDto>>> GetHostelRoomsReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<PendingHostelApplicationItemDto>>> GetPendingHostelApplicationsReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<LabDirectoryItemDto>>> GetLabDirectoryReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<VenueUtilizationItemDto>>> GetVenueUtilizationReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<PendingStudentRegistrationItemDto>>> GetPendingStudentRegistrationsReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<CertificateTypeCatalogItemDto>>> GetCertificateTypesCatalogReportAsync(ReportFilterDto filter);
        Task<ServiceResult<PagedReportResultDto<ComplaintCategorySlaItemDto>>> GetComplaintCategoriesSlaReportAsync(ReportFilterDto filter);
    }
}
