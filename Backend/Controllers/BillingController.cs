using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.Billing;
using CampusServicesPortal.DTOs.Responses.Billing;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Authorize]
    [Route("api/billing")]
    public class BillingController : BaseApiController
    {
        private readonly IBillingService _billingService;

        public BillingController(IBillingService billingService)
        {
            _billingService = billingService;
        }

        // GET /api/billing/ledger — Unified: View statement statement logs [PDF: 0.1.14]
        [HttpGet("ledger")]
        public async Task<IActionResult> GetMyLedger([FromQuery] int? studentId)
        {
            int targetStudentId;

            // FIXED: If user is an Admin and passed an explicit query parameter, use that ID! [INDEX]
            if (User.IsInRole("Admin") && studentId.HasValue && studentId.Value > 0)
            {
                targetStudentId = studentId.Value;
            }
            else
            {
                // Fallback for regular students viewing their own profile token context [INDEX]
                targetStudentId = GetCurrentStudentId();
            }

            var result = await _billingService.GetStudentLedgerAsync(targetStudentId);
            return ProcessServiceResult(result, "Account transaction ledger statements retrieved successfully.");
        }

        // POST /api/billing/payments/{id}/request-otp — Student: Request 3D Secure OTP for payment authorization
        [HttpPost("payments/{id}/request-otp")]
        public async Task<IActionResult> RequestPaymentOtp(int id)
        {
            int studentId = GetCurrentStudentId();
            var result = await _billingService.RequestPaymentOtpAsync(id, studentId);
            return ProcessServiceResult(result, "3D Secure payment OTP requested successfully.");
        }

        // POST /api/billing/payments/{id}/pay — Student: Settle invoice via checkout simulation [PDF: 0.1.14]
        [HttpPost("payments/{id}/pay")]
        public async Task<IActionResult> PayInvoice(int id, [FromBody] ProcessPaymentRequestDto? request = null)
        {
            int studentId = GetCurrentStudentId();
            var result = await _billingService.ProcessPaymentAsync(id, studentId, request);
            return ProcessServiceResult(result, "Financial clearing transaction processed and completed successfully.");
        }

        // POST /api/billing/fees/assign — Admin: Allocate standard fees (Single or Faculty Bulk Run) [PDF: 0.1.15]
        [Authorize(Roles = "Admin")]
        [HttpPost("fees/assign")]
        public async Task<IActionResult> AssignFee([FromBody] AssignFeeDto request)
        {
            var result = await _billingService.AssignFeeAsync(request);
            return ProcessServiceResult(result, "University billing framework run executed successfully.");
        }

        // POST /api/billing/fines — Admin: Log a laboratory fine sanction track [PDF: 0.1.16]
        [Authorize(Roles = "Admin")]
        [HttpPost("fines")]
        public async Task<IActionResult> IssueFine([FromBody] GenerateLabFineDto request)
        {
            var result = await _billingService.IssueLabFineAsync(request);
            return ProcessServiceResult(result, "Laboratory regulatory fine issued and logged successfully.");
        }

        private int GetCurrentStudentId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
                throw new UnauthorizedAccessException("Claim identity configuration error: User is not authenticated.");

            return int.Parse(claim.Value);
        }
    }
}
