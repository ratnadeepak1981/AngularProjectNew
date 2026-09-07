using System.Threading.Tasks;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Compiles dynamic account parameters into the HTML verification document layout.
        /// </summary>
        Task<ServiceResult<string>> GenerateVerificationEmailPreviewAsync(string email);

        /// <summary>
        /// Signs into the production Gmail SMTP gateway over Port 465 to send a live email.
        /// </summary>
        Task SendEmailAsync(string recipientEmail, string messageSubject, string HTMLContent);
    }
}
