using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Hostel.Application;
using CampusServicesPortal.DTOs.Responses.Hostel.Application;
using CampusServicesPortal.DTOs.Requests.Nortifcation; // Maps cleanly to your CreateNotificationDto namespace
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Implementations
{
    public class HostelService : IHostelService
    {
        private readonly IHostelRepository _hostelRepository;
        // Injecting centralized notification service contract architecture layer [INDEX]
        private readonly INotificationService _notificationService;

        public HostelService(IHostelRepository hostelRepository, INotificationService notificationService)
        {
            _hostelRepository = hostelRepository;
            _notificationService = notificationService;
        }

        private static readonly System.Threading.SemaphoreSlim _submitLock = new System.Threading.SemaphoreSlim(1, 1);

        public async Task<ServiceResult<HostelApplicationResponseDto>> SubmitApplicationAsync(int studentId, SubmitHostelApplicationDto request)
        {
            await _submitLock.WaitAsync();
            try
            {
                var existingApplication = await _hostelRepository.GetActiveApplicationByStudentIdAsync(studentId);
                if (existingApplication != null)
                {
                    return ServiceResult<HostelApplicationResponseDto>.Failure("Operation Blocked. You already have an active or processed hostel application track record.", 400);
                }

                var application = new HostelApplication
                {
                    StudentId = studentId,
                    PreferredHostelId = request.HostelId,
                    TermSemester = request.TermSemester,
                    SpecialRequirements = request.SpecialRequirements,
                    Status = "Pending"
                };

                await _hostelRepository.AddApplicationAsync(application);
                await _hostelRepository.SaveChangesAsync();

                var freshRecord = await _hostelRepository.GetApplicationByIdAsync(application.Id);
                return ServiceResult<HostelApplicationResponseDto>.Success(MapToResponseDto(freshRecord!), 201);
            }
            finally
            {
                _submitLock.Release();
            }
        }

        public async Task<ServiceResult<HostelApplicationResponseDto>> UpdateStatusAsync(int applicationId, UpdateHostelStatusDto request)
        {
            var application = await _hostelRepository.GetApplicationByIdAsync(applicationId);
            if (application == null)
            {
                return ServiceResult<HostelApplicationResponseDto>.Failure("Hostel application profile record not found.", 404);
            }

            // Clean validation parsing matching BRD requirements
            string incomingStatus = request.Status.Trim();
            if (incomingStatus != "Approved" && incomingStatus != "Rejected")
            {
                return ServiceResult<HostelApplicationResponseDto>.Failure("Invalid transaction state payload. Status parameter must be 'Approved' or 'Rejected'.", 400);
            }

            // 1. Stage the application state modification vector in shared context tracking memory pool
            application.Status = incomingStatus;

            if (incomingStatus == "Rejected")
            {
                application.AssignedRoomId = null;
            }

            // 2. AUTOMATED TRIGGER: Formulate and stage notification parameters inside the same pool cache [INDEX]
            string notifType = incomingStatus == "Approved" ? "HostelApplicationApproved" : "HostelApplicationRejected";
            string notifMessage = incomingStatus == "Approved"
                ? "Your accommodation request for Hostel Alpha has been approved. Room assignment pending."
                : "Your hostel allocation request was refused due to maximum room capacity thresholds being reached.";

            // Dispatches alert staging over decoupled Notification Service pipeline [INDEX]
            await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
            {
                StudentId = application.StudentId,
                Type = notifType,
                Message = notifMessage
            });

            // 3. ACID ATOMIC SCOPE RECONCILIATION FLUSH [INDEX]
            // Commits both application state changes and notification rows inside one atomic transaction round-trip! [INDEX]
            await _hostelRepository.SaveChangesAsync();

            return ServiceResult<HostelApplicationResponseDto>.Success(MapToResponseDto(application), 200);
        }

        public async Task<ServiceResult<HostelApplicationResponseDto>> AssignRoomAsync(int applicationId, AssignRoomDto request)
        {
            try
            {
                var application = await _hostelRepository.GetApplicationByIdAsync(applicationId);
                if (application == null)
                {
                    return ServiceResult<HostelApplicationResponseDto>.Failure("Hostel application profile record not found.", 404);
                }

                // Rule #3: Application must be Pending, Approved, or RoomAssigned before room assignment
                if (application.Status != "Approved" && application.Status != "RoomAssigned" && application.Status != "Pending")
                {
                    return ServiceResult<HostelApplicationResponseDto>.Failure("Transaction Aborted. Room assignment is only permitted for active or pending applications.", 400);
                }

                var room = await _hostelRepository.GetRoomByIdAsync(request.RoomId);
                if (room == null || !room.IsActive)
                {
                    return ServiceResult<HostelApplicationResponseDto>.Failure("Target allocation room coordinate record does not exist or is inactive.", 404);
                }

                // Verify the room actually resides within the student's targeted hostel choice block
                if (room.HostelId != application.PreferredHostelId)
                {
                    return ServiceResult<HostelApplicationResponseDto>.Failure("Location Mismatch. The selected room does not reside within the preferred hostel facility.", 400);
                }

                // Room Capacity Guard: Compute current load against architectural ceilings
                int activeOccupants = await _hostelRepository.GetRoomCurrentOccupancyAsync(request.RoomId);
                bool isReassigningSameRoom = (application.AssignedRoomId == request.RoomId);
                int effectiveOccupants = isReassigningSameRoom ? activeOccupants - 1 : activeOccupants;

                if (effectiveOccupants >= room.MaxCapacity)
                {
                    return ServiceResult<HostelApplicationResponseDto>.Failure($"Allocation Denied. Room {room.RoomNumber} has reached its maximum physical capacity limit of {room.MaxCapacity}.", 409);
                }

                // 1. Stage status updates to RoomAssigned state row properties in shared memory pool
                application.AssignedRoomId = request.RoomId;
                application.Status = "RoomAssigned";

                // 2. AUTOMATED TRIGGER: Stage distinct alert details tracking physical room codes
                await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
                {
                    StudentId = application.StudentId,
                    Type = "HostelRoomAssigned",
                    Message = $"Your dynamic accommodation keys have been verified. You are assigned to Room Number: {room.RoomNumber}."
                });

                // 3. ACID ATOMIC COMMIT: Commits room assignment AND notification atomically
                await _hostelRepository.SaveChangesAsync();

                // Fetch updated state graph to load fresh room details
                var completeRecord = await _hostelRepository.GetApplicationByIdAsync(applicationId);
                return ServiceResult<HostelApplicationResponseDto>.Success(MapToResponseDto(completeRecord!), 200);
            }
            catch (Exception ex)
            {
                return ServiceResult<HostelApplicationResponseDto>.Failure($"Operation Failed: {ex.Message}", 500);
            }
        }

        public async Task<ServiceResult<IEnumerable<HostelApplicationResponseDto>>> GetStudentApplicationsAsync(int studentId)
        {
            var applications = await _hostelRepository.GetAllApplicationsAsync();
            var filtered = applications.Where(a => a.StudentId == studentId).Select(MapToResponseDto);
            return ServiceResult<IEnumerable<HostelApplicationResponseDto>>.Success(filtered, 200);
        }

        public async Task<ServiceResult<IEnumerable<HostelApplicationResponseDto>>> GetAllPendingApplicationsAsync()
        {
            var applications = await _hostelRepository.GetAllApplicationsAsync();
            var pending = applications.Where(a => a.Status == "Pending").Select(MapToResponseDto);
            return ServiceResult<IEnumerable<HostelApplicationResponseDto>>.Success(pending, 200);
        }

        public async Task<ServiceResult<IEnumerable<HostelApplicationResponseDto>>> GetAllApplicationsAsync()
        {
            var applications = await _hostelRepository.GetAllApplicationsAsync();
            var allMapped = applications.Select(MapToResponseDto);
            return ServiceResult<IEnumerable<HostelApplicationResponseDto>>.Success(allMapped, 200);
        }

        public async Task<ServiceResult<IEnumerable<HostelLookupResponseDto>>> GetAllActiveHostelsAsync()
        {
            var masterList = await _hostelRepository.GetAllHostelsAsync();

            var lookupDtos = new List<HostelLookupResponseDto>();
            foreach (var h in masterList)
            {
                var roomDtos = new List<RoomLookupResponseDto>();
                foreach (var r in h.Rooms ?? new List<Room>())
                {
                    int occupancy = await _hostelRepository.GetRoomCurrentOccupancyAsync(r.Id);
                    roomDtos.Add(new RoomLookupResponseDto
                    {
                        Id = r.Id,
                        RoomNumber = r.RoomNumber,
                        MaxCapacity = r.MaxCapacity,
                        CurrentOccupancy = occupancy
                    });
                }
                lookupDtos.Add(new HostelLookupResponseDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    Rooms = roomDtos
                });
            }

            return ServiceResult<IEnumerable<HostelLookupResponseDto>>.Success(lookupDtos, 200);
        }

        private static HostelApplicationResponseDto MapToResponseDto(HostelApplication app)
        {
            return new HostelApplicationResponseDto
            {
                Id = app.Id,
                StudentId = app.StudentId,
                StudentName = app.Student?.FullName ?? "Unknown Student",
                HostelName = app.PreferredHostel?.Name ?? "Unknown Building",
                RoomNumber = app.AssignedRoom?.RoomNumber,
                TermSemester = app.TermSemester,
                SpecialRequirements = app.SpecialRequirements,
                Status = app.Status,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
