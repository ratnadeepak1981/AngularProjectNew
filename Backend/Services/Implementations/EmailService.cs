using System;
using System.IO;
using System.Threading.Tasks;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MimeKit;

namespace CampusServicesPortal.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IAccountRepository _accountRepo;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(
            IAccountRepository accountRepo,
            IWebHostEnvironment env,
            ILogger<EmailService> logger,
            IConfiguration configuration)
        {
            _accountRepo = accountRepo;
            _env = env;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Production-ready live Gmail SMTP delivery tunnel via Port 465 Implicit SSL.
        /// </summary>
        public async Task SendEmailAsync(string recipientEmail, string messageSubject, string HTMLContent)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            string host = smtpSettings["Host"] ?? "://gmail.com";
            int port = int.Parse(smtpSettings["Port"] ?? "465");
            string senderEmail = smtpSettings["SenderEmail"] ?? throw new InvalidOperationException("SMTP Configuration error: SenderEmail is missing.");
            string senderName = smtpSettings["SenderName"] ?? "Campus Services Portal";
            string appPassword = smtpSettings["AppPassword"] ?? throw new InvalidOperationException("SMTP Configuration error: AppPassword token is missing.");

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(senderName, senderEmail));
            emailMessage.To.Add(new MailboxAddress("Portal Student User", recipientEmail));
            emailMessage.Subject = messageSubject;

            var bodyBuilder = new BodyBuilder { HtmlBody = HTMLContent };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // Enforce secure Implicit SSL handshake tunnel parameters on connect
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(senderEmail, appPassword);
                await client.SendAsync(emailMessage);

                _logger.LogInformation("SMTP Production Channel: Email successfully delivered to {Recipient}.", recipientEmail);
            }
            catch (MailKit.Security.AuthenticationException authEx)
            {
                _logger.LogError("❌ SMTP CONFIG AUTH FAIL: Your 16-character App Password was actively rejected by Google's servers.");
                _logger.LogError("Diagnostic Error Details: {Message}", authEx.Message);
            }
            catch (SmtpCommandException cmdEx)
            {
                if (cmdEx.ErrorCode == SmtpErrorCode.RecipientNotAccepted)
                {
                    _logger.LogError("❌ SMTP RECIPIENT REJECTED: The destination email address '{Recipient}' was flagged as invalid or not found by the mail server.", recipientEmail);
                }
                else
                {
                    _logger.LogError("❌ SMTP PROTOCOL COMMAND ERROR: The mail gateway encountered an instruction failure.");
                }
                _logger.LogError("Diagnostic Error Details: {Message} (Status Code: {StatusCode})", cmdEx.Message, (int)cmdEx.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ SMTP HOST OR SOCKET EXCEPTION: Network infrastructure failed to process the request payload.");
                _logger.LogError("Diagnostic Error Type: {ErrorType} - Message: {Message}", ex.GetType().Name, ex.Message);
            }
            finally
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
        }

        /// <summary>
        /// Reads active registration verification parameters directly out of database tables and compiles the template.
        /// </summary>
        public async Task<ServiceResult<string>> GenerateVerificationEmailPreviewAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<string>.Failure("Email parameter is required.", 400);
            }

            string cleanEmail = email.Trim();
            var student = await _accountRepo.GetStudentByEmailThroughUserAsync(cleanEmail);

            if (student == null)
            {
                return ServiceResult<string>.Failure($"No student account record found for email '{cleanEmail}'.", 404);
            }

            string token = student.EmailVerificationToken ?? "NO-TOKEN-GENERATED";
            string expiresStr = student.EmailVerificationTokenExpiresAt.HasValue
                ? student.EmailVerificationTokenExpiresAt.Value.ToString("g") + " UTC"
                : "24 Hours from Registration";

            string statusBadge = student.EmailVerified
                ? "<span style='background: #dcfce7; color: #15803d; padding: 4px 12px; border-radius: 12px; font-weight: bold; font-size: 12px;'>VERIFIED & ACTIVE</span>"
                : "<span style='background: #fef3c7; color: #b45309; padding: 4px 12px; border-radius: 12px; font-weight: bold; font-size: 12px;'>UNVERIFIED (PENDING TOKEN)</span>";

            string templatePath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Email", "EmailVerification.cshtml");
            string cssPath = Path.Combine(_env.ContentRootPath, "Views", "Templates", "Email", "EmailVerification.css");

            if (!File.Exists(templatePath) || !File.Exists(cssPath))
            {
                return ServiceResult<string>.Failure("Email verification template files missing on server.", 500);
            }

            string cssContent = await File.ReadAllTextAsync(cssPath);
            string htmlTemplate = await File.ReadAllTextAsync(templatePath);

            string renderedHtml = htmlTemplate
                .Replace("{{CSS_CONTENT}}", cssContent, StringComparison.Ordinal)
                .Replace("{{STATUS_BADGE}}", statusBadge, StringComparison.Ordinal)
                .Replace("{{FULL_NAME}}", student.FullName ?? "Student User", StringComparison.Ordinal)
                .Replace("{{INDEX_NUMBER}}", student.IndexNumber ?? "N/A", StringComparison.Ordinal)
                .Replace("{{REGISTERED_EMAIL}}", student.User?.Email ?? cleanEmail, StringComparison.Ordinal)
                .Replace("{{FACULTY_NAME}}", student.Faculty?.Name ?? "General University Faculty", StringComparison.Ordinal)
                .Replace("{{TOKEN}}", token, StringComparison.Ordinal)
                .Replace("{{EXPIRES_STR}}", expiresStr, StringComparison.Ordinal);

            return ServiceResult<string>.Success(renderedHtml, 200);
        }
    }
}
