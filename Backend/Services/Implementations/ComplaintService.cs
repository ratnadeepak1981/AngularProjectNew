using CampusServicesPortal.DTOs.Requests.Complaints;
using CampusServicesPortal.DTOs.Requests.Nortifcation;
using CampusServicesPortal.DTOs.Responses.Complaints;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Implementations
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepository;
        // Injecting centralized notification service contract architecture layer [INDEX]
        private readonly INotificationService _notificationService;

        public ComplaintService(IComplaintRepository complaintRepository, INotificationService notificationService)
        {
            _complaintRepository = complaintRepository;
            _notificationService = notificationService;
        }

        public async Task<ServiceResult<ComplaintResponseDto>>
            SubmitComplaintAsync(int studentId, SubmitComplaintDto request)
        {
            bool studentExists =  await _complaintRepository.StudentExistsAsync(studentId);

            if (!studentExists)
            {
                return ServiceResult<ComplaintResponseDto>
                    .Failure("Student not found.", 404);
            }

            var category =await _complaintRepository.GetCategoryByIdAsync(request.CategoryId);

            if (category == null)
            {
                return ServiceResult<ComplaintResponseDto> .Failure("Complaint category not found.",404);
            }

            if (!category.IsActive)
            {
                return ServiceResult<ComplaintResponseDto>.Failure("The selected complaint category is inactive.",400);
            }

            if (await _complaintRepository.HasDuplicatePendingComplaintAsync(studentId, request.CategoryId, request.Description))
            {
                return ServiceResult<ComplaintResponseDto>.Failure("You already have an identical pending complaint ticket submitted for this category.", 409);
            }

            var complaint = new Complaint
            {
                StudentId = studentId,
                CategoryId = request.CategoryId,
                Description = request.Description.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _complaintRepository.AddComplaintAsync(complaint);

            await _complaintRepository.SaveChangesAsync();

            var savedComplaint =await _complaintRepository.GetComplaintByIdAsync(complaint.Id);

            var response =MapComplaint(savedComplaint ?? complaint);

            return ServiceResult<ComplaintResponseDto>.Success(response, 201);
        }

        public async Task<ServiceResult<IEnumerable<ComplaintResponseDto>>> GetStudentComplaintsAsync(int studentId)
        {
            bool studentExists =
                await _complaintRepository
                    .StudentExistsAsync(studentId);

            if (!studentExists)
            {
                return ServiceResult<
                    IEnumerable<ComplaintResponseDto>>
                    .Failure("Student not found.", 404);
            }

            var complaints =
                await _complaintRepository
                    .GetComplaintsByStudentIdAsync(studentId);

            var response = complaints
                .Select(MapComplaint)
                .ToList();

            return ServiceResult<
                IEnumerable<ComplaintResponseDto>>
                .Success(response, 200);
        }

        public async Task<ServiceResult< IEnumerable<ComplaintResponseDto>>> GetComplaintsAsync(string? status)
        {
            string? normalizedStatus = null;

            if (!string.IsNullOrWhiteSpace(status))
            {
                normalizedStatus = NormalizeStatus(status);

                if (normalizedStatus == null)
                {
                    return ServiceResult<
                        IEnumerable<ComplaintResponseDto>>
                        .Failure(
                            "Status must be Pending, In Progress, or Resolved.",
                            400);
                }
            }

            var complaints =await _complaintRepository
                    .GetComplaintsAsync(normalizedStatus);

            var response = complaints.Select(MapComplaint).ToList();

            return ServiceResult<IEnumerable<ComplaintResponseDto>>.Success(response, 200);
        }

        public async Task<ServiceResult<ComplaintResponseDto>> UpdateComplaintStatusAsync(int complaintId, UpdateComplaintStatusDto request)
        {
            // 1. Fetch the tracking record using your existing repository method
            var complaint = await _complaintRepository.GetComplaintByIdAsync(complaintId);
            if (complaint == null)
            {
                return ServiceResult<ComplaintResponseDto>.Failure("Grievance case profile record not found.", 404);
            }

            string incomingStatus = request.Status.Trim();
            if (incomingStatus != "In Progress" && incomingStatus != "Resolved" && incomingStatus != "Rejected")
            {
                return ServiceResult<ComplaintResponseDto>.Failure("Invalid status parameter. Status must be 'In Progress', 'Resolved', or 'Rejected'.", 400);
            }

            // BRD Rule: Enforce one-way status progression (Pending → In Progress → Resolved/Rejected)
            if (!IsValidStatusTransition(complaint.Status, incomingStatus))
            {
                return ServiceResult<ComplaintResponseDto>.Failure(
                    $"Invalid status transition. Cannot move from '{complaint.Status}' to '{incomingStatus}'. Resolved and Rejected complaints are closed.", 400);
            }

            if (incomingStatus == "Resolved" && (string.IsNullOrWhiteSpace(request.ResolutionNote) || request.ResolutionNote.Trim().Length < 5))
            {
                return ServiceResult<ComplaintResponseDto>.Failure(
                    "A resolution note of at least 5 characters is required when resolving a complaint ticket.", 400);
            }

            // 2. Mutate the tracked properties (EF Core tracks this change in memory automatically!)
            complaint.Status = incomingStatus;
            complaint.ResolutionNote = request.ResolutionNote?.Trim();

            // 3. AUTOMATED TRIGGER: Resolve notification text parameters dynamically
            string cleanRemarksSnippet = !string.IsNullOrWhiteSpace(complaint.ResolutionNote)
                ? $" Resolution Note: \"{complaint.ResolutionNote}\""
                : "";

            string descriptionPreview = complaint.Description.Length > 40
                ? complaint.Description.Substring(0, 37) + "..."
                : complaint.Description;

            string notificationMessage = incomingStatus switch
            {
                "In Progress" => $"Maintenance Update: Your logged grievance ticket regarding '{descriptionPreview}' has been reviewed and is now officially In Progress.{cleanRemarksSnippet}",
                "Resolved" => $"Resolution Confirmed: Your submitted complaint regarding '{descriptionPreview}' has been successfully resolved and closed.{cleanRemarksSnippet}",
                "Rejected" => $"Ticket Notice: Your logged complaint regarding '{descriptionPreview}' was rejected by the administration team.{cleanRemarksSnippet}",
                _ => $"Your submitted complaint ticket status has been updated to {incomingStatus}."
            };

            // Stage the new notification alert model into the same tracking cache pool
            await _notificationService.SendInternalNotificationAsync(new CreateNotificationDto
            {
                StudentId = complaint.StudentId,
                Type = "ComplaintStatusUpdated",
                Message = notificationMessage
            });

            // 4. ACID ATOMIC SCOPE RECONCILIATION FLUSH
            // Uses your existing repository save method to commit BOTH changes inside a single SQL transaction block!
            await _complaintRepository.SaveChangesAsync();

            return ServiceResult<ComplaintResponseDto>.Success(MapComplaint(complaint), 200);
        }




        public async Task<ServiceResult<IEnumerable<ComplaintCategoryResponseDto>>> GetActiveCategoriesAsync()
        {
            var categories =
                await _complaintRepository
                    .GetActiveCategoriesAsync();

            var response = categories
                .Select(MapCategory)
                .ToList();

            return ServiceResult<
                IEnumerable<ComplaintCategoryResponseDto>>
                .Success(response, 200);
        }

        public async Task<ServiceResult<ComplaintCategoryResponseDto>> CreateCategoryAsync(CreateComplaintCategoryDto request)
        {
            string name = request.Name.Trim();

            bool nameExists =
                await _complaintRepository
                    .CategoryNameExistsAsync(name);

            if (nameExists)
            {
                return ServiceResult<
                    ComplaintCategoryResponseDto>
                    .Failure(
                        "A complaint category with this name already exists.",
                        409);
            }

            var category = new ComplaintCategory
            {
                Name = name,
                IsActive = true
            };

            await _complaintRepository
                .AddCategoryAsync(category);

            await _complaintRepository.SaveChangesAsync();

            return ServiceResult<
                ComplaintCategoryResponseDto>
                .Success(
                    MapCategory(category),
                    201);
        }

        public async Task<
            ServiceResult<ComplaintCategoryResponseDto>>
            UpdateCategoryAsync(int categoryId,UpdateComplaintCategoryDto request)
        {
            var category =
                await _complaintRepository
                    .GetCategoryByIdAsync(categoryId);

            if (category == null)
            {
                return ServiceResult<
                    ComplaintCategoryResponseDto>
                    .Failure(
                        "Complaint category not found.",
                        404);
            }

            string name = request.Name.Trim();

            bool nameExists =
                await _complaintRepository
                    .CategoryNameExistsAsync(
                        name,
                        categoryId);

            if (nameExists)
            {
                return ServiceResult<
                    ComplaintCategoryResponseDto>
                    .Failure(
                        "A complaint category with this name already exists.",
                        409);
            }

            category.Name = name;
            category.IsActive = request.IsActive;

            _complaintRepository.UpdateCategory(category);

            await _complaintRepository.SaveChangesAsync();

            return ServiceResult<
                ComplaintCategoryResponseDto>
                .Success(
                    MapCategory(category),
                    200);
        }

        public async Task<ServiceResult<bool>> DeleteCategoryAsync(int categoryId)
        {
            var category =
                await _complaintRepository
                    .GetCategoryByIdAsync(categoryId);

            if (category == null)
            {
                return ServiceResult<bool>
                    .Failure(
                        "Complaint category not found.",
                        404);
            }

            category.IsActive = false;
            _complaintRepository.UpdateCategory(category);
            await _complaintRepository.SaveChangesAsync();

            return ServiceResult<bool>
                .Success(true, 200);
        }

        private static ComplaintResponseDto MapComplaint(Complaint complaint)
        {
            return new ComplaintResponseDto
            {
                Id = complaint.Id,
                StudentId = complaint.StudentId,
                StudentName =
                    complaint.Student?.FullName
                    ?? string.Empty,
                CategoryId = complaint.CategoryId,
                CategoryName =
                    complaint.Category?.Name
                    ?? string.Empty,
                Description = complaint.Description,
                Status = complaint.Status,
                ResolutionNote =
                    complaint.ResolutionNote,
                CreatedAt = complaint.CreatedAt
            };
        }

        private static ComplaintCategoryResponseDto
            MapCategory(ComplaintCategory category)
        {
            return new ComplaintCategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive
            };
        }

        private static string?
            NormalizeStatus(string status)
        {
            return status
                .Trim()
                .ToLowerInvariant() switch
            {
                "pending" => "Pending",

                "in progress" => "In Progress",

                "resolved" => "Resolved",

                _ => null
            };
        }

        private static bool IsValidStatusTransition(
            string currentStatus,
            string nextStatus)
        {
            // BRD Rule: One-way progression only. Resolved and Rejected are terminal states.
            return currentStatus switch
            {
                "Pending" => nextStatus is "In Progress" or "Rejected",
                "In Progress" => nextStatus is "Resolved" or "Rejected",
                _ => false // Resolved and Rejected are terminal — no further transitions
            };
        }
    }
}