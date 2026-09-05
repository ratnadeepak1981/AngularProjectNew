using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Reports;
using CampusServicesPortal.DTOs.Responses.Reports;

namespace CampusServicesPortal.Repositories.Interfaces
{
    public interface IReportRepository
    {
        Task<InstitutionalKpiReportDto> GetInstitutionalKpiSummaryAsync();
        Task<PagedReportResultDto<StudentDetailItemDto>> GetStudentReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<HostelDetailItemDto>> GetHostelReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<LabBookingDetailItemDto>> GetLabReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<PaymentDetailItemDto>> GetBillingReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<ComplaintDetailItemDto>> GetComplaintReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<CertificateRequestDetailItemDto>> GetCertificateReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<EventRegistrationDetailItemDto>> GetEventReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<NotificationDetailItemDto>> GetNotificationReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<HostelRoomDetailItemDto>> GetHostelRoomsReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<PendingHostelApplicationItemDto>> GetPendingHostelApplicationsReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<LabDirectoryItemDto>> GetLabDirectoryReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<VenueUtilizationItemDto>> GetVenueUtilizationReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<PendingStudentRegistrationItemDto>> GetPendingStudentRegistrationsReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<CertificateTypeCatalogItemDto>> GetCertificateTypesCatalogReportAsync(ReportFilterDto filter);
        Task<PagedReportResultDto<ComplaintCategorySlaItemDto>> GetComplaintCategoriesSlaReportAsync(ReportFilterDto filter);
    }
}
