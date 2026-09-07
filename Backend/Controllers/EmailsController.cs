using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/email")]
    public class EmailsController : BaseApiController
    {
        private readonly IEmailService _emailService;

        public EmailsController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // GET /api/email/preview/verification — Render HTML verification email preview directly inside Swagger
        [HttpGet("preview/verification")]
        public async Task<IActionResult> PreviewVerificationEmail([FromQuery] string email)
        {
            var result = await _emailService.GenerateVerificationEmailPreviewAsync(email);
            if (!result.IsSuccess)
            {
                return ProcessServiceResult(result, "Email preview generation failed.");
            }

            return Content(result.Data ?? string.Empty, "text/html");
        }

        // POST /api/email/send/verification — Dispatches the authentic email directly via real Gmail SMTP
        [HttpPost("send/verification")]
        public async Task<IActionResult> SendVerificationEmail([FromQuery] string email)
        {
            // 1. Compile the custom template payload directly out of the database records
            var previewResult = await _emailService.GenerateVerificationEmailPreviewAsync(email);

            if (!previewResult.IsSuccess)
            {
                return ProcessServiceResult(previewResult, "Failed to compile template payload for the requested account.");
            }

            // 2. Fire live traffic down the implicit SSL pipeline
            string subject = "Campus Services Portal - Verify Your Student Email Account";
            string htmlContent = previewResult.Data ?? string.Empty;

            await _emailService.SendEmailAsync(email, subject, htmlContent);

            return Ok(new
            {
                status = "PROCESSED",
                message = $"Verification security routing engine run completed for inbox target '{email}'."
            });
        }

        // GET /api/email/preview/password-reset — Render HTML password reset email preview directly inside Swagger
        [HttpGet("preview/password-reset")]
        public async Task<IActionResult> PreviewPasswordResetEmail([FromQuery] string email)
        {
            var result = await _emailService.GeneratePasswordResetEmailPreviewAsync(email);
            if (!result.IsSuccess)
            {
                return ProcessServiceResult(result, "Password reset email preview generation failed.");
            }

            return Content(result.Data ?? string.Empty, "text/html");
        }

        // POST /api/email/send/password-reset — Dispatches the authentic password reset email directly via real Gmail SMTP
        [HttpPost("send/password-reset")]
        public async Task<IActionResult> SendPasswordResetEmail([FromQuery] string email)
        {
            var previewResult = await _emailService.GeneratePasswordResetEmailPreviewAsync(email);
            if (!previewResult.IsSuccess)
            {
                return ProcessServiceResult(previewResult, "Failed to compile password reset template payload for the requested account.");
            }

            string subject = "Campus Services Portal - Reset Your Account Password";
            string htmlContent = previewResult.Data ?? string.Empty;

            await _emailService.SendEmailAsync(email, subject, htmlContent);

            return Ok(new
            {
                status = "PROCESSED",
                message = $"Password reset security notification dispatched to inbox target '{email}'."
            });
        }
    }
}
