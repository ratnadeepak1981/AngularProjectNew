using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Auth;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Interfaces
{
    public interface IPasswordService
    {
        Task<ServiceResult<object>> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<ServiceResult<object>> VerifyResetOtpAsync(VerifyResetOtpRequestDto request);
        Task<ServiceResult<object>> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<ServiceResult<object>> ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
        Task<ServiceResult<object>> AdminResetStudentPasswordAsync(int studentId);
        Task<ServiceResult<DTOs.Responses.Auth.PasswordPolicyResponseDto>> GetPasswordPolicyAsync();
    }
}
