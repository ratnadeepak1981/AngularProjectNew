using System;
using System.Collections.Generic;

namespace CampusServicesPortal.DTOs.Responses.Reports
{
    public class EventSummaryItemDto
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public int VenueCapacity { get; set; }
        public int EventCapacity { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int TotalRegistrations { get; set; }
        public int ConfirmedRegistrations { get; set; }
        public int ActiveHolds { get; set; }
        public double FillRate { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class EventRegistrationDetailItemDto
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string IndexNumber { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class EventReportDto
    {
        public List<EventSummaryItemDto> EventSummaries { get; set; } = new List<EventSummaryItemDto>();
        public int GrandTotalEvents { get; set; }
        public int GrandTotalUpcoming { get; set; }
        public int GrandTotalCompleted { get; set; }
        public int GrandTotalRegistrations { get; set; }
    }

    public class VenueUtilizationItemDto
    {
        public int VenueId { get; set; }
        public string VenueCode { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string VenueType { get; set; } = "Auditorium"; // Auditorium, Lecture Hall, Seminar Room, Open Ground
        public bool HasProjector { get; set; } = true;
        public bool HasSoundSystem { get; set; } = true;
        public int TotalEventsHosted { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class VenueUtilizationReportDto
    {
        public int GrandTotalVenues { get; set; }
        public int GrandTotalSeatingCapacity { get; set; }
        public int TotalEventsHostedYtd { get; set; }
        public double AverageCapacityPerVenue { get; set; }
    }
}
