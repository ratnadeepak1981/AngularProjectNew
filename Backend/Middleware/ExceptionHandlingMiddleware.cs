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

            // 🌟 STEP 1: INTERCEPT DATABASE UNIQUE CONSTRAINT VIOLATIONS
            if (exception is DbUpdateException dbUpdateEx && dbUpdateEx.InnerException is SqlException sqlEx)
            {
                // 2627: Unique Constraint violation, 2601: Unique Index violation
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    statusCode = HttpStatusCode.Conflict; // 409 Conflict Status Code
                    friendlyMessage = ResolveUniqueConstraintMessage(sqlEx.Message);
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

        // 🌟 STEP 2: PARSE SQL CONSTRAINT INDEX NAMES FOR USER-FRIENDLY ALERTS
        private string ResolveUniqueConstraintMessage(string sqlMessage)
        {
            if (sqlMessage.Contains("IX_Venues_Name"))
                return "An event venue with this exact name already exists in the system master directory.";

            if (sqlMessage.Contains("IX_Hostels_Name"))
                return "A hostel building profile with this exact name already exists in the administrative list.";

            if (sqlMessage.Contains("IX_Labs_Name"))
                return "A laboratory facility slot with this exact name is already registered.";

            if (sqlMessage.Contains("IX_StudentPhoneNumbers_PhoneNumber"))
                return "This primary contact telephone number is already linked to another student account.";

            if (sqlMessage.Contains("IX_FeeTypes_Name"))
                return "A billing ledger fee category configuration with this exact name already exists.";

            if (sqlMessage.Contains("UX_Rooms_Hostel_RoomNumber"))
                return "This specific room number has already been allocated within the chosen hostel building.";

            if (sqlMessage.Contains("UX_Events_Venue_Schedule"))
                return "Scheduling Collision! This physical venue is already booked for another event at the specified date and time.";

            return "A data registration conflict occurred. A record with duplicate unique tracking fields already exists.";
        }
    }
}
