using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class PagedReportResultDto<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public int StartRecord => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
        public int EndRecord => Math.Min(PageNumber * PageSize, TotalCount);

        // Crystal Report Summary and Subreport Grand Totals
        public object? SummaryData { get; set; }
        public object? GrandTotals { get; set; }
    }
}
