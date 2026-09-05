using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class CertificateTypeSummaryItemDto
    {
        public int CertificateTypeId { get; set; }
        public string CertificateTypeName { get; set; } = string.Empty;
        public int TotalRequested { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public double ApprovalRate { get; set; }
    }

    public class CertificateRequestDetailItemDto
    {
        public int RequestId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string IndexNumber { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public int CertificateTypeId { get; set; }
        public string CertificateTypeName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }

    public class CertificateReportDto
    {
        public List<CertificateTypeSummaryItemDto> TypeSummaries { get; set; } = new List<CertificateTypeSummaryItemDto>();
        public int GrandTotalRequests { get; set; }
        public int GrandTotalPending { get; set; }
        public int GrandTotalApproved { get; set; }
        public int GrandTotalRejected { get; set; }
        public double OverallApprovalPercentage { get; set; }
    }

    public class CertificateTypeCatalogItemDto
    {
        public int CertificateTypeId { get; set; }
        public string CertificateTypeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public int ProcessingSlaDays { get; set; } = 3;
        public int TotalRequestsAllTime { get; set; }
        public int ApprovedRequestsCount { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CertificateTypeCatalogReportDto
    {
        public int GrandTotalCertificateTypes { get; set; }
        public decimal AverageFee { get; set; }
        public double AverageSlaDays { get; set; }
        public int TotalRequestsProcessed { get; set; }
    }
}
