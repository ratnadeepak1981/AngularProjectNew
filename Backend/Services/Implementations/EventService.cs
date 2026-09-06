using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusServicesPortal.Data;
using CampusServicesPortal.DTOs.Requests.Events;
using CampusServicesPortal.DTOs.Requests.Nortifcation;
using CampusServicesPortal.DTOs.Responses.Events;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.EntityFrameworkCore;

namespace CampusServicesPortal.Services.Implementations
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly INotificationService _notificationService;
        private readonly AppDbContext _context;

        public EventService(IEventRepository eventRepository, INotificationService notificationService, AppDbContext context)
        {
            _eventRepository = eventRepository;
            _notificationService = notificationService;
            _context = context;
        }

        public async Task<ServiceResult<EventResponseDto>> CreateEventAsync(CreateEventDto request)
        {
            // 1. Verify destination venue exists
            var venue = await _eventRepository.GetVenueByIdAsync(request.VenueId);
            if (venue == null)
                return ServiceResult<EventResponseDto>.Failure("The selected venue does not exist in the master records.", 404);

            // 2. Rule #13: Event capacity must never exceed structural space limits [PDF: 0.1.11, 0.1.20]
            if (request.Capacity > venue.Capacity)
                return ServiceResult<EventResponseDto>.Failure($"Validation Failed. Event capacity ({request.Capacity}) exceeds maximum structural limits of {venue.Name} ({venue.Capacity}).", 400);

            // 3. Ensure logical time range boundaries
            if (request.StartDateTime >= request.EndDateTime)
                return ServiceResult<EventResponseDto>.Failure("Scheduling Error. Start time must occur before the end time.", 400);

            // 4. Rule #6: Prevent venue schedule clashes on concurrent calendar bookings [PDF: 0.1.10, 0.1.20]
            bool isOverlapping = await _eventRepository.IsVenueDoubleBookedAsync(request.VenueId, request.StartDateTime, request.EndDateTime);
            if (isOverlapping)
                return ServiceResult<EventResponseDto>.Failure("Scheduling Conflict. The selected venue is already booked for an overlapping time window.", 409);

            // 5. Build entity and commit
            var newEvent = new Event
            {
                VenueId = request.VenueId,
                Title = request.Title,
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Capacity = request.Capacity,
                Description = request.Description
            };

            await _eventRepository.AddEventAsync(newEvent);
            await _eventRepository.SaveChangesAsync();

            var response = MapToResponseDto(newEvent, venue.Name, 0);
            return ServiceResult<EventResponseDto>.Success(response, 201);
        }

        public async Task<ServiceResult<EventResponseDto>> RegisterForEventAsync(int studentId, RegisterEventDto request)
        {
            // 1. Fetch targeted event parameters
            var targetEvent = await _eventRepository.GetEventByIdAsync(request.EventId);
            if (targetEvent == null)
                return ServiceResult<EventResponseDto>.Failure("The requested event does not exist.", 404);

            if (targetEvent.EndDateTime < DateTime.UtcNow)
                return ServiceResult<EventResponseDto>.Failure("Registration Closed. This event has already concluded.", 400);

            // 2. Rule #1: Guard against duplicate registrations [PDF: 0.1.10, 0.1.19]
            var existingRegistration = await _eventRepository.GetRegistrationAsync(request.EventId, studentId);
            if (existingRegistration != null)
                return ServiceResult<EventResponseDto>.Failure("Operation Rejected. You are already registered for this event.", 400);

            // 3. Rule #7: Block additions once the event hits its target capacity ceiling [PDF: 0.1.10, 0.1.20]
            int currentRegisteredCount = await _eventRepository.GetRegistrationCountAsync(request.EventId);
            if (currentRegisteredCount >= targetEvent.Capacity)
                return ServiceResult<EventResponseDto>.Failure("Registration Full. This event has reached its maximum capacity limit.", 409);

            // 4. Calculate time-bound temporary hold expiration window from System Settings (BRD Rule #12)
            var holdSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "LabBookingHoldMinutes" || s.SettingKey == "reservation-hold-minutes");
            int holdMinutes = (holdSetting != null && int.TryParse(holdSetting.SettingValue, out int val) && val > 0) ? val : 15;
            DateTime expiresAt = DateTime.UtcNow.AddMinutes(holdMinutes);

            // 5. Save registration record as a temporary 'Held' state [PDF: 0.1.12]
            var registration = new EventRegistration
            {
                EventId = request.EventId,
                StudentId = studentId,
                Status = "Held",
                ExpiresAt = expiresAt
            };

            await _eventRepository.AddRegistrationAsync(registration);

            // =========================================================================
            // 🚀 AUTOMATED TRIGGER: Stage your unread notification alert card [INDEX]
            // =========================================================================
            string eventTitle = targetEvent.Title ?? "University Event";
            string locationVenue = targetEvent.Venue?.Name ?? "Main Campus Grounds";

            // Call your decoupled notification service pipeline to queue the model entry
            await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
            {
                StudentId = studentId, // Targets exclusively the single registering student [INDEX]
                Type = "EventRegistrationHeld",
                Message = $"Reservation hold initiated! A seat for '{eventTitle}' at venue '{locationVenue}' has been temporarily locked for you. Complete confirmation within 15 minutes."
            });
            // =========================================================================

            // 6. ACID ATOMIC RECONCILIATION FLUSH [INDEX]
            // Commits BOTH rows concurrently inside a single implicit native SQL transaction! [INDEX]
            await _eventRepository.SaveChangesAsync();

            var response = MapToResponseDto(targetEvent, targetEvent.Venue?.Name ?? "Unknown Venue", currentRegisteredCount + 1);
            return ServiceResult<EventResponseDto>.Success(response, 201);
        }


        public async Task<ServiceResult<object>> CancelRegistrationAsync(int eventId, int studentId)
        {
            // 1. Fetch matching user allocation
            var registration = await _eventRepository.GetRegistrationAsync(eventId, studentId);
            if (registration == null)
                return ServiceResult<object>.Failure("Registration record not found for this student.", 404);

            // 2. Remove registration track row
            _eventRepository.RemoveRegistration(registration);
            await _eventRepository.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Event registration cancelled successfully." }, 200);
        }

        public async Task<ServiceResult<IEnumerable<EventResponseDto>>> GetAvailableEventsAsync(int? studentId = null)
        {
            var events = await _eventRepository.GetUpcomingEventsAsync();
            var responseList = new List<EventResponseDto>();

            foreach (var ev in events)
            {
                int registeredCount = await _eventRepository.GetRegistrationCountAsync(ev.Id);
                bool isReg = false;
                if (studentId.HasValue && studentId.Value > 0)
                {
                    var existingReg = await _eventRepository.GetRegistrationAsync(ev.Id, studentId.Value);
                    isReg = (existingReg != null);
                }

                var dto = MapToResponseDto(ev, ev.Venue?.Name ?? "Unknown Venue", registeredCount);
                dto.IsRegistered = isReg;
                responseList.Add(dto);
            }

            return ServiceResult<IEnumerable<EventResponseDto>>.Success(responseList, 200);
        }

        private static EventResponseDto MapToResponseDto(Event ev, string venueName, int registeredCount)
        {
            return new EventResponseDto
            {
                Id = ev.Id,
                Title = ev.Title,
                VenueName = venueName,
                StartDateTime = ev.StartDateTime,
                EndDateTime = ev.EndDateTime,
                Capacity = ev.Capacity,
                RegisteredCount = registeredCount,
                Description = ev.Description,
                UsesReservedSeating = false // Default fallback until schema extends [PDF: 0.1.12]
            };
        }

        public async Task<ServiceResult<IEnumerable<object>>> GetEventRegistrationsAsync(int eventId)
        {
            var regs = await _eventRepository.GetEventRegistrationsAsync(eventId);
            var dtos = regs.Select(r => new
            {
                r.Id,
                r.EventId,
                r.StudentId,
                Student = r.Student != null ? new
                {
                    r.Student.Id,
                    r.Student.IndexNumber,
                    r.Student.FullName
                } : null,
                r.Status,
                r.ExpiresAt
            });
            return ServiceResult<IEnumerable<object>>.Success(dtos, 200);
        }

        // Inside EventService.cs - Ensure 'public' modifier is explicitly written
        public async Task ProcessExpiredHoldsAsync()
        {
            var expiredHolds = await _eventRepository.GetExpiredRegistrationHoldsAsync(DateTime.UtcNow);
            if (expiredHolds.Any())
            {
                foreach (var hold in expiredHolds)
                {
                    hold.Status = "Expired";
                    _eventRepository.UpdateRegistrationStatus(hold);
                }
                await _eventRepository.SaveChangesAsync();
            }
        }
    }
}
