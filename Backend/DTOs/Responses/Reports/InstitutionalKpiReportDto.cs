using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class InstitutionalKpiReportDto
    {
        // 1. Student Master & Profile
        public int TotalRegisteredStudents { get; set; }
        public int ActiveStudents { get; set; }
        public int DeactivatedStudents { get; set; }
        public int UnverifiedStudents { get; set; }
        public int MasterListIntakeTotal { get; set; }

        // 2. Hostel Accommodation
        public int TotalHostels { get; set; }
        public int TotalBedsCapacity { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public double BedOccupancyPercentage { get; set; }
        public int PendingHostelApplications { get; set; }
        public int UnallocatedStudentsCount { get; set; }

        // 3. Lab Reservations
        public int TotalLabs { get; set; }
        public int TotalLabSeats { get; set; }
        public int ConfirmedLabBookings { get; set; }
        public int ActiveLabHolds { get; set; }
        public int ExpiredOrCancelledBookings { get; set; }
        public double LabUtilizationPercentage { get; set; }

        // 4. Financial & Billing
        public decimal TotalBilledAmount { get; set; }
        public decimal TotalCollectedAmount { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public int TotalUnpaidInvoices { get; set; }
        public int TotalPaidInvoices { get; set; }
        public double CollectionRatePercentage { get; set; }

        // 5. Grievances & Complaints
        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public double ComplaintResolutionRatePercentage { get; set; }

        // 6. Certificate Requests
        public int TotalCertificateRequests { get; set; }
        public int PendingCertificateRequests { get; set; }
        public int ApprovedCertificateRequests { get; set; }
        public int RejectedCertificateRequests { get; set; }

        // 7. Events & Venues
        public int TotalVenues { get; set; }
        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int CompletedEvents { get; set; }
        public int TotalEventRegistrations { get; set; }

        // 8. Notifications & Alerts
        public int TotalNotificationsSent { get; set; }
        public int UnreadNotificationsCount { get; set; }
        public int ReadNotificationsCount { get; set; }

        // 9. Timestamp & Verification Stamp
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
