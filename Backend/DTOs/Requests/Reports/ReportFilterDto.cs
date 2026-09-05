using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Requests.Reports
{
    public class ReportFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? StartDate { get => DateFrom; set => DateFrom = value; }
        public DateTime? EndDate { get => DateTo; set => DateTo = value; }
        public string? SearchTerm { get; set; }

        // Single / Multi-select filters
        public int? FacultyId { get; set; }
        public int? HostelId { get; set; }
        public int? LabId { get; set; }
        public int? CategoryId { get; set; }
        public string? Status { get; set; }

        public List<int>? FacultyIds { get; set; }
        public List<string>? Statuses { get; set; }
        public List<string>? TermSemesters { get; set; }
        public List<int>? HostelIds { get; set; }
        public List<int>? LabIds { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? CertificateTypeIds { get; set; }
        public List<int>? FeeTypeIds { get; set; }
        public List<int>? EventIds { get; set; }

        // Specific subreport drilldown key
        public string? DrilldownKey { get; set; }
        public int? DrilldownId { get; set; }

        // Pagination & Sorting
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } = "desc";
    }
}
