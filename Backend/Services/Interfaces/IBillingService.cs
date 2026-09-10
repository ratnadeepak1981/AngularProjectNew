using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Billing;
using CampusServicesPortal.DTOs.Responses.Billing;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Interfaces
{
    public interface IBillingService
    {
        // Handles bulk pricing updates or manual balance tracking creations
        Task<ServiceResult<object>> AssignFeeAsync(AssignFeeDto request);

        Task<ServiceResult<IEnumerable<FeePaymentResponseDto>>> GetAllFeeAssignmentsAsync();


        // Hooks operational rule violations immediately out to financial profiles
        Task<ServiceResult<object>> IssueLabFineAsync(GenerateLabFineDto request);

        // Requests server-authoritative 3D Secure payment OTP
        Task<ServiceResult<object>> RequestPaymentOtpAsync(int paymentId, int studentId);

        // Simulates balance checking and settlement transactions
        Task<ServiceResult<FeePaymentResponseDto>> ProcessPaymentAsync(int paymentId, int studentId, ProcessPaymentRequestDto? request = null);

        // Account statements lookups
        Task<ServiceResult<IEnumerable<FeePaymentResponseDto>>> GetStudentLedgerAsync(int studentId);
    }
}
