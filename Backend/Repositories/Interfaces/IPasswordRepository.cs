using System.Threading.Tasks;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories.Interfaces
{
    public interface IPasswordRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<Student?> GetStudentByUserIdAsync(int userId);
        Task<Student?> GetStudentByEmailThroughUserAsync(string email);
        Task InvalidateExistingResetTokensAsync(int studentId);
        Task SavePasswordResetTokenAsync(PasswordResetToken token);
        Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token);
        Task<PasswordResetToken?> GetLatestUnusedTokenAsync();
        Task<Student?> GetStudentByIdAsync(int id);
        Task UpdateUserAsync(User user);
        Task UpdateResetTokenStatusAsync(PasswordResetToken token);
        Task RevokeAllUserSessionsAsync(int userId);
        Task<SystemSetting?> GetSystemSettingAsync(string key);
        Task<IEnumerable<PasswordHistory>> GetRecentPasswordHistoriesAsync(int userId, int limit);
        Task AddPasswordHistoryAsync(PasswordHistory history);
    }
}
