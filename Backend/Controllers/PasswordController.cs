using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.Auth;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [ApiController]
    [Route("api/password")]
    [Route("api/auth")]
    public class PasswordController : BaseApiController
    {
        private readonly IPasswordService _passwordService;
        private readonly IPasswordRepository _passwordRepository;

        public PasswordController(IPasswordService passwordService, IPasswordRepository passwordRepository)
        {
            _passwordService = passwordService;
            _passwordRepository = passwordRepository;
        }

        // GET /api/password/policy or /api/auth/password-policy
        [AllowAnonymous]
        [HttpGet("policy")]
        [HttpGet("password-policy")]
        public async Task<IActionResult> GetPasswordPolicy()
        {
            var result = await _passwordService.GetPasswordPolicyAsync();
            return ProcessServiceResult(result, "System password security policy retrieved.");
        }

        // POST /api/password/forgot-password or /api/auth/forgot-password
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            var result = await _passwordService.ForgotPasswordAsync(request);
            return ProcessServiceResult(result, "Password reset OTP dispatched successfully.");
        }

        // POST /api/password/verify-reset-otp or /api/auth/verify-reset-otp
        [AllowAnonymous]
        [HttpPost("verify-reset-otp")]
        public async Task<IActionResult> VerifyResetOtp([FromBody] VerifyResetOtpRequestDto request)
        {
            var result = await _passwordService.VerifyResetOtpAsync(request);
            return ProcessServiceResult(result, "Password reset OTP verified successfully.");
        }

        // POST /api/password/reset-password or /api/auth/reset-password
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            var result = await _passwordService.ResetPasswordAsync(request);
            return ProcessServiceResult(result, "Password reset and updated successfully.");
        }

        // POST /api/password/change-password or /api/auth/change-password
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            int userId = 0;
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int uid))
            {
                userId = uid;
            }
            else
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(emailClaim))
                {
                    var user = await _passwordRepository.GetUserByEmailAsync(emailClaim);
                    userId = user?.Id ?? 0;
                }
            }

            if (userId <= 0)
            {
                return Unauthorized("Unable to resolve authenticated user identity.");
            }

            var result = await _passwordService.ChangePasswordAsync(userId, request);
            return ProcessServiceResult(result, "Password changed successfully.");
        }
    }
}
