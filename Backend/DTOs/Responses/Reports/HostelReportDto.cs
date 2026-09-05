using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class HostelSummaryItemDto
    {
        public int HostelId { get; set; }
        public string HostelName { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public int TotalBedCapacity { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public double OccupancyRate { get; set; }
        public int PendingApplicationsCount { get; set; }
    }

    public class HostelDetailItemDto
    {
        public int ApplicationId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string IndexNumber { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string PreferredHostelName { get; set; } = string.Empty;
        public string TermSemester { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssignedRoomNumber { get; set; }
        public string? AssignedHostelName { get; set; }
        public DateTime ApplicationDate { get; set; }
    }

    public class HostelReportDto
    {
        public List<HostelSummaryItemDto> HostelSummaries { get; set; } = new List<HostelSummaryItemDto>();
        public int GrandTotalBeds { get; set; }
        public int GrandTotalOccupied { get; set; }
        public int GrandTotalAvailable { get; set; }
        public double OverallOccupancyPercentage { get; set; }
        public int TotalUnallocatedStudents { get; set; }
    }

    public class HostelRoomDetailItemDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int HostelId { get; set; }
        public string HostelName { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public int Capacity { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds { get; set; }
        public string RoomType { get; set; } = string.Empty; // Single, Double, Triple, Shared
        public string Status { get; set; } = "Active"; // Active, Maintenance
        public decimal FeePerSemester { get; set; }
    }

    public class HostelRoomReportDto
    {
        public int GrandTotalRooms { get; set; }
        public int GrandTotalBeds { get; set; }
        public int GrandTotalOccupied { get; set; }
        public int GrandTotalVacant { get; set; }
        public double OverallOccupancyPercentage { get; set; }
        public int MaintenanceRoomsCount { get; set; }
    }

    public class PendingHostelApplicationItemDto
    {
        public int ApplicationId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string IndexNumber { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string PreferredHostelName { get; set; } = string.Empty;
        public string RequestedRoomType { get; set; } = string.Empty;
        public string TermSemester { get; set; } = string.Empty;
        public DateTime ApplicationDate { get; set; }
        public double DistanceScore { get; set; }
        public string Status { get; set; } = "Pending";
        public string PaymentVerificationStatus { get; set; } = "Pending";
    }

    public class PendingHostelAppReportDto
    {
        public int TotalPendingApplications { get; set; }
        public int TotalAllocatedThisTerm { get; set; }
        public int TotalRejectedThisTerm { get; set; }
        public int OldestPendingDays { get; set; }
    }
}
