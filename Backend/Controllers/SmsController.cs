using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.Sms;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/sms")]
    public class SmsController : BaseApiController
    {
        private readonly ISmsService _smsService;

        public SmsController(ISmsService smsService)
        {
            _smsService = smsService;
        }

        // POST /api/sms/send — Shared SMS dispatch endpoint for Password OTP, Payment OTP, Receipts, and Alerts
        [HttpPost("send")]
        public async Task<IActionResult> DispatchSms([FromBody] SendSmsRequestDto request)
        {
            var result = await _smsService.DispatchSmsAsync(request);
            return ProcessServiceResult(result, "SMS dispatched successfully.");
        }

        // GET /api/sms/preview/forgot-password?email=... — Render HTML SMS simulation preview for Forgot Password OTP
        [HttpGet("preview/forgot-password")]
        public async Task<IActionResult> PreviewForgotPasswordSms([FromQuery] string email)
        {
            var result = await _smsService.GenerateForgotPasswordSmsPreviewAsync(email);
            if (!result.IsSuccess)
            {
                return ProcessServiceResult(result, "SMS preview generation failed.");
            }

            return Content(result.Data ?? string.Empty, "text/html");
        }

        // GET /api/sms/preview/payment-otp?phoneNumber=... — Render HTML SMS simulation preview for Payment Verification OTP
        [HttpGet("preview/payment-otp")]
        public async Task<IActionResult> PreviewPaymentOtpSms([FromQuery] string? phoneNumber = null)
        {
            var result = await _smsService.GeneratePaymentOtpSmsPreviewAsync(phoneNumber);
            if (!result.IsSuccess)
            {
                return ProcessServiceResult(result, "Payment OTP SMS preview generation failed.");
            }

            return Content(result.Data ?? string.Empty, "text/html");
        }

        // GET /api/sms/preview/payment-receipt?email=...&amount=5000 — Render HTML SMS simulation preview for Fee Payment Receipt
        [HttpGet("preview/payment-receipt")]
        public async Task<IActionResult> PreviewPaymentReceiptSms([FromQuery] string email, [FromQuery] decimal? amount, [FromQuery] string? transactionId)
        {
            var result = await _smsService.GeneratePaymentReceiptSmsPreviewAsync(email, amount, transactionId);
            if (!result.IsSuccess)
            {
                return ProcessServiceResult(result, "Payment receipt SMS preview generation failed.");
            }

            return Content(result.Data ?? string.Empty, "text/html");
        }

        // GET /api/sms/preview/phone-otp?phoneNumber=... — Render HTML SMS simulation preview for Mobile Phone OTP
        [HttpGet("preview/phone-otp")]
        public async Task<IActionResult> PreviewPhoneOtpSms([FromQuery] string? phoneNumber = null)
        {
            var result = await _smsService.GeneratePhoneOtpSmsPreviewAsync(phoneNumber);
            if (!result.IsSuccess)
            {
                return ProcessServiceResult(result, "Phone OTP SMS preview generation failed.");
            }

            return Content(result.Data ?? string.Empty, "text/html");
        }
    }
}
