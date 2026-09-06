using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Sms;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Interfaces
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<ServiceResult<object>> DispatchSmsAsync(SendSmsRequestDto request);
        Task<ServiceResult<string>> GenerateForgotPasswordSmsPreviewAsync(string email);
        Task<ServiceResult<string>> GeneratePaymentOtpSmsPreviewAsync(string? phoneNumber = null);
        Task<ServiceResult<string>> GeneratePaymentReceiptSmsPreviewAsync(string email, decimal? amount = null, string? transactionId = null);
        Task<ServiceResult<string>> GeneratePhoneOtpSmsPreviewAsync(string? phoneNumber = null);
    }
}
