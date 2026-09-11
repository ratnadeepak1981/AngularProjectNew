using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories.Interfaces
{
    public interface IAdminManagementRepository
    {
        Task<IEnumerable<User>> GetAdminsAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task AddUserAsync(User user);
        void UpdateUser(User user);
        void DeleteUser(User user);
        Task<int> CountActiveSuperAdminsAsync();
        Task SaveChangesAsync();
    }
}
