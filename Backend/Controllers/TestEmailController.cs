using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; // Added for reading appsettings
using Microsoft.Extensions.Logging;       // Added for clean console tracing
using MailKit.Net.Smtp;
using MimeKit;

namespace CampusServicesPortal.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/test-email")]
    public class TestEmailController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TestEmailController> _logger;

        // Injected constructor properties to safely access system settings configuration trees
        public TestEmailController(IConfiguration configuration, ILogger<TestEmailController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // POST /api/test-email/verify-credentials-from-config
        [HttpPost("verify-credentials-from-config")]
        public async Task<IActionResult> TestCredentialsFromConfig()
        {
            // 1. Pull settings dynamically out of your corrected appsettings.json file
            var smtpSettings = _configuration.GetSection("SmtpSettings");

            string host = smtpSettings["Host"];
            int port = int.Parse(smtpSettings["Port"] ?? "465");
            string senderEmail = smtpSettings["SenderEmail"];
            string senderName = smtpSettings["SenderName"] ?? "Campus Services Portal";
            string appPassword = smtpSettings["AppPassword"]?.Replace(" ", ""); // Strips spaces automatically

            _logger.LogInformation("⏳ Swagger Check: Initiating dynamic SMTP config validation test...");

            // 2. Build the structural MimeKit message envelope
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(senderName, senderEmail));
            emailMessage.To.Add(new MailboxAddress("Portal Configuration Test", senderEmail)); // Sends directly to yourself
            emailMessage.Subject = "Campus Portal - Dynamic Configuration Connectivity Test";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = "<h3>Configuration Binding Layer Verification Successful! Your appsettings.json maps perfectly.</h3>"
            };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            // 3. Fire-and-forget down your configuration-driven pipeline
            using var client = new SmtpClient();
            try
            {
                // Enforces Port 465 Implicit SSL security option strings
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(senderEmail, appPassword);
                await client.SendAsync(emailMessage);

                return Ok(new
                {
                    Status = "SUCCESS",
                    Message = "✅ Connected, authenticated, and transmitted perfectly using appsettings.json! Your configuration layer works flawlessly.",
                    ParsedHost = host,
                    ParsedPort = port,
                    SenderUsed = senderEmail
                });
            }
            catch (MailKit.Security.AuthenticationException authEx)
            {
                _logger.LogError("❌ SMTP CONFIG AUTH FAIL: {Msg}", authEx.Message);
                return BadRequest(new
                {
                    Status = "AUTHENTICATION_FAILED",
                    Message = "❌ Google rejected the App Password value extracted from your appsettings.json file.",
                    Details = authEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("⚠️ CONFIG NETWORK FAULT: {Msg}", ex.Message);
                return StatusCode(500, new
                {
                    Status = "CONFIGURATION_OR_NETWORK_ERROR",
                    Message = "⚠️ Could not connect to the server using the settings provided.",
                    Details = ex.Message,
                    ErrorType = ex.GetType().Name
                });
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}

