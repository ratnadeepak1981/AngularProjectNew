using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class LabSummaryItemDto
    {
        public int LabId { get; set; }
        public string LabName { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int TotalConfiguredSeats { get; set; }
        public int ConfirmedBookings { get; set; }
        public int ActiveHolds { get; set; }
        public int CancelledOrExpired { get; set; }
        public double UtilizationRate { get; set; }
    }

    public class LabBookingDetailItemDto
    {
        public int BookingId { get; set; }
        public int LabId { get; set; }
        public string LabName { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string IndexNumber { get; set; } = string.Empty;
        public string? SeatNumber { get; set; }
        public DateTime BookingDate { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class LabReportDto
    {
        public List<LabSummaryItemDto> LabSummaries { get; set; } = new List<LabSummaryItemDto>();
        public int GrandTotalLabs { get; set; }
        public int GrandTotalCapacity { get; set; }
        public int GrandTotalBookings { get; set; }
        public int GrandTotalActiveHolds { get; set; }
        public double OverallUtilizationPercentage { get; set; }
    }

    public class LabDirectoryItemDto
    {
        public int LabId { get; set; }
        public string LabCode { get; set; } = string.Empty;
        public string LabName { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int TotalConfiguredSeats { get; set; }
        public int ActiveOperationalSeats { get; set; }
        public int MaintenanceSeats { get; set; }
        public string? SupervisorName { get; set; }
        public string? OperatingHours { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class LabDirectoryReportDto
    {
        public int GrandTotalLabs { get; set; }
        public int GrandTotalWorkstations { get; set; }
        public int OperationalWorkstations { get; set; }
        public int MaintenanceWorkstations { get; set; }
        public double OperationalPercentage { get; set; }
    }
}
