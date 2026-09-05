using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class ComplaintCategorySummaryItemDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int TotalFiled { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
        public double ResolutionRate { get; set; }
    }

    public class ComplaintDetailItemDto
    {
        public int ComplaintId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string IndexNumber { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ComplaintReportDto
    {
        public List<ComplaintCategorySummaryItemDto> CategorySummaries { get; set; } = new List<ComplaintCategorySummaryItemDto>();
        public int GrandTotalComplaints { get; set; }
        public int GrandTotalPending { get; set; }
        public int GrandTotalInProgress { get; set; }
        public int GrandTotalResolved { get; set; }
        public double OverallResolutionPercentage { get; set; }
    }

    public class ComplaintCategorySlaItemDto
    {
        public int CategoryId { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TargetSlaHours { get; set; } = 48;
        public int TotalFiled { get; set; }
        public int ResolvedOnTime { get; set; }
        public int BreachedSlaCount { get; set; }
        public int ActiveOpenCount { get; set; }
        public double SlaComplianceRate { get; set; }
    }

    public class ComplaintCategorySlaReportDto
    {
        public int GrandTotalCategories { get; set; }
        public int TotalGrievancesLogged { get; set; }
        public double OverallSlaCompliancePercentage { get; set; }
        public int TotalBreachedTickets { get; set; }
    }
}
