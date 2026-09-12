using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore; // 🌟 ADDED: Required to detect DbUpdateException
using Microsoft.Data.SqlClient;       // 🌟 ADDED: Required to analyze SqlException codes
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled system exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            // Default Fallback Configurations
            var statusCode = HttpStatusCode.InternalServerError; // 500
            var friendlyMessage = "A critical system crash occurred while processing your request.";

            // 🌟 STEP 1: INTERCEPT CUSTOM DOMAIN EXCEPTIONS (Duplicate & Conflict Checks)
            if (exception is CampusServicesPortal.Exceptions.DuplicateBookingException dupEx)
            {
                statusCode = HttpStatusCode.Conflict; // 409 Conflict Status Code
                friendlyMessage = dupEx.Message;
            }
            else if (exception is InvalidOperationException invEx)
            {
                statusCode = HttpStatusCode.Conflict; // 409 Conflict Status Code
                friendlyMessage = invEx.Message;
            }
            // 🌟 STEP 2: INTERCEPT UNCAUGHT DATABASE UNIQUE CONSTRAINT VIOLATIONS (Errors 2601, 2627, 547)
            else if (exception is DbUpdateException dbUpdateEx && dbUpdateEx.InnerException is SqlException sqlEx)
            {
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    statusCode = HttpStatusCode.Conflict; // 409 Conflict Status Code
                    friendlyMessage = ResolveUniqueConstraintMessage(sqlEx.Message);
                }
                else if (sqlEx.Number == 547)
                {
                    statusCode = HttpStatusCode.Conflict; // 409 Conflict Status Code
                    friendlyMessage = ResolveForeignKeyMessage(sqlEx.Message);
                }
            }

            context.Response.StatusCode = (int)statusCode;

            var response = new ErrorResponseWrapper<object>
            {
                Succeeded = false,
                Message = friendlyMessage,
                Data = null,
                Errors = new List<string>()
            };

            // Rule: Populates error array details dynamically based on environment hosting state
            if (_env.IsDevelopment())
            {
                response.Errors.Add($"Exception: {exception.Message}");
                if (exception.InnerException != null)
                {
                    response.Errors.Add($"InnerException: {exception.InnerException.Message}");
                }
                response.Errors.Add($"StackTrace: {exception.StackTrace}");
            }
            else
            {
                response.Errors.Add(statusCode == HttpStatusCode.Conflict
                    ? "Data conflict constraint check triggered."
                    : "Internal server error. Contact the system administrator.");
            }

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var jsonResult = JsonSerializer.Serialize(response, options);

            await context.Response.WriteAsync(jsonResult);
        }

        // 🌟 STEP 3: PARSE SQL CONSTRAINT INDEX NAMES FOR USER-FRIENDLY ALERTS
        private string ResolveUniqueConstraintMessage(string sqlMessage)
        {
            if (sqlMessage.Contains("UX_LabBookings_Student_ActiveSlot"))
                return "You already hold an active booking slot for this specific date and time frame.";

            if (sqlMessage.Contains("UX_LabBookings_Seat_ActiveSlot"))
                return "This specific lab seat is already reserved by another student for this slot.";

            if (sqlMessage.Contains("IX_Venues_Name"))
                return "An event venue with this exact name already exists in the system master directory.";

            if (sqlMessage.Contains("IX_Hostels_Name"))
                return "A hostel building profile with this exact name already exists in the administrative list.";

            if (sqlMessage.Contains("IX_Labs_Name"))
                return "A laboratory facility slot with this exact name is already registered.";

            if (sqlMessage.Contains("IX_StudentPhoneNumbers_PhoneNumber") || sqlMessage.Contains("UQ_StudentPhoneNumbers_PhoneNumber"))
                return "This primary contact telephone number is already linked to another student account.";

            if (sqlMessage.Contains("IX_FeeTypes_Name"))
                return "A billing ledger fee category configuration with this exact name already exists.";

            if (sqlMessage.Contains("UX_Rooms_Hostel_RoomNumber"))
                return "This specific room number has already been allocated within the chosen hostel building.";

            if (sqlMessage.Contains("UX_Events_Venue_Schedule"))
                return "Scheduling Collision! This physical venue is already booked for another event at the specified date and time.";

            if (sqlMessage.Contains("IX_Students_IndexNumber"))
                return "This Student Index Number is already allocated to an active profile container.";

            if (sqlMessage.Contains("IX_Students_ContactDetails"))
                return "This primary contact telephone number string is already registered to another user profile.";

            if (sqlMessage.Contains("IX_CertificateTypes_Name"))
                return "A certificate type with this title already exists.";

            if (sqlMessage.Contains("UX_CertificateRequests_Student_Pending"))
                return "A pending request already exists for this certificate type.";

            if (sqlMessage.Contains("IX_ComplaintCategories_Name"))
                return "A complaint category with this name already exists.";

            if (sqlMessage.Contains("IX_Faculties_Name"))
                return "A faculty with this designated title already exists.";

            if (sqlMessage.Contains("UX_EventRegistrations_Event_Student"))
                return "You are already registered for this event.";

            return "A data registration conflict occurred. A record with duplicate unique tracking fields already exists.";
        }

        private string ResolveForeignKeyMessage(string sqlMessage)
        {
            if (sqlMessage.Contains("FK_Rooms_Hostels_HostelId"))
                return "Cannot delete this hostel because it still contains assigned rooms.";

            if (sqlMessage.Contains("FK_HostelApplications_Rooms_AssignedRoomId"))
                return "Cannot delete or close this room because students are currently assigned to it.";

            if (sqlMessage.Contains("FK_StudentMasterLists_Faculties_FacultyId"))
                return "Cannot delete this faculty because it is assigned to an active student master tracking list.";

            if (sqlMessage.Contains("FK_Events_Venues_VenueId"))
                return "Cannot delete this venue because there are active events scheduled to take place inside it.";

            if (sqlMessage.Contains("DELETE statement conflicted"))
                return "This record cannot be deleted because it is currently linked to active operational data.";

            return "Failed to save changes because a related referenced entity record could not be found.";
        }
    }
}
