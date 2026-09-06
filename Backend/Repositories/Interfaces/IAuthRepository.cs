using System.Threading.Tasks;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<Student?> GetStudentByUserIdWithFacultyAsync(int userId);
        Task UpdateUserAsync(User user);
        Task<SystemSetting?> GetSystemSettingAsync(string key);

        // Refresh Token operations
        Task SaveRefreshTokenAsync(RefreshToken token);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task UpdateRefreshTokenAsync(RefreshToken token);
        Task RevokeUserRefreshTokensAsync(int userId);
    }
}
