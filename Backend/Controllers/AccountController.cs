using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.Auth;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Route("api/account")]
    [Route("api/auth")]
    public class AccountController : BaseApiController
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        // POST /api/account/verify-email or /api/auth/verify-email
        [AllowAnonymous]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request)
        {
            var result = await _accountService.VerifyEmailAsync(request);
            return ProcessServiceResult(result, "Email address confirmed and verified successfully.");
        }

        // POST /api/account/resend-verification or /api/auth/resend-verification
        [AllowAnonymous]
        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequestDto request)
        {
            var result = await _accountService.ResendVerificationEmailAsync(request);
            return ProcessServiceResult(result, "A fresh email verification link has been dispatched.");
        }

        // POST /api/account/send-phone-otp or /api/auth/send-phone-otp
        [AllowAnonymous]
        [HttpPost("send-phone-otp")]
        public async Task<IActionResult> SendPhoneOtp([FromBody] SendPhoneOtpRequestDto request)
        {
            var result = await _accountService.SendPhoneOtpAsync(request);
            return ProcessServiceResult(result, "SMS verification OTP dispatched successfully.");
        }

        // POST /api/account/verify-phone-otp or /api/auth/verify-phone-otp
        [AllowAnonymous]
        [HttpPost("verify-phone-otp")]
        public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyPhoneOtpRequestDto request)
        {
            var result = await _accountService.VerifyPhoneOtpAsync(request);
            return ProcessServiceResult(result, "Primary mobile number verified and activated successfully.");
        }

        // GET /api/account/deactivate-check/{studentId} or /api/auth/deactivate-check/{studentId}
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("deactivate-check/{studentId}")]
        public async Task<IActionResult> CheckDeactivation(int studentId)
        {
            var result = await _accountService.CheckDeactivationEligibilityAsync(studentId);
            return ProcessServiceResult(result, "Student account deactivation eligibility evaluation complete.");
        }

        // POST /api/account/deactivate/{studentId} or /api/auth/deactivate/{studentId}
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("deactivate/{studentId}")]
        public async Task<IActionResult> DeactivateAccount(int studentId)
        {
            var result = await _accountService.DeactivateAccountAsync(studentId);
            return ProcessServiceResult(result, "Student account profile has been deactivated successfully.");
        }

        // POST /api/account/reactivate/{studentId} or /api/auth/reactivate/{studentId}
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("reactivate/{studentId}")]
        public async Task<IActionResult> ReactivateAccount(int studentId)
        {
            var result = await _accountService.ReactivateAccountAsync(studentId);
            return ProcessServiceResult(result, "Student account profile has been reactivated successfully.");
        }
    }
}
