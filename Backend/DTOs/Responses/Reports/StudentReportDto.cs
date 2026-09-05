using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class StudentSummaryItemDto
    {
        public int FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public int TotalEnrolled { get; set; }
        public int ActiveCount { get; set; }
        public int DeactivatedCount { get; set; }
        public int UnverifiedCount { get; set; }
        public double PercentageOfTotal { get; set; }
    }

    public class StudentDetailItemDto
    {
        public int StudentId { get; set; }
        public string IndexNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? City { get; set; }
        public string Status { get; set; } = string.Empty; // Active / Deactivated / Unverified
        public bool EmailVerified { get; set; }
        public DateTime? DeactivatedAt { get; set; }
    }

    public class StudentReportDto
    {
        public List<StudentSummaryItemDto> FacultySummaries { get; set; } = new List<StudentSummaryItemDto>();
        public int GrandTotalStudents { get; set; }
        public int GrandTotalActive { get; set; }
        public int GrandTotalDeactivated { get; set; }
        public int GrandTotalUnverified { get; set; }
    }

    public class PendingStudentRegistrationItemDto
    {
        public int StudentId { get; set; }
        public string IndexNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string VerificationStatus { get; set; } = "Pending"; // Unverified, Pending Documents, Pending Approval
        public string MissingDocuments { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
    }

    public class PendingStudentRegistrationReportDto
    {
        public int GrandTotalPendingRegistrations { get; set; }
        public int UnverifiedEmailCount { get; set; }
        public int MissingDocumentsCount { get; set; }
        public int PendingApprovalCount { get; set; }
    }
}
