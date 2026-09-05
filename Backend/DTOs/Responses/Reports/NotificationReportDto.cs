using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class NotificationTypeSummaryItemDto
    {
        public string Type { get; set; } = string.Empty;
        public int TotalSent { get; set; }
        public int ReadCount { get; set; }
        public int UnreadCount { get; set; }
        public double ReadRate { get; set; }
    }

    public class NotificationDetailItemDto
    {
        public int NotificationId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string IndexNumber { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationReportDto
    {
        public List<NotificationTypeSummaryItemDto> TypeSummaries { get; set; } = new List<NotificationTypeSummaryItemDto>();
        public int GrandTotalNotifications { get; set; }
        public int GrandTotalRead { get; set; }
        public int GrandTotalUnread { get; set; }
        public double OverallReadPercentage { get; set; }
    }
}
