using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetAllAsync();

        Task<IEnumerable<NotificationWithStudent>> GetAllWithStudentDetailsAsync();

        Task<IEnumerable<Notification>> GetByStudentIdAsync(int studentId);
        Task<Notification?> GetByIdAndStudentIdAsync(int id, int studentId);
        Task<Notification?> GetByIdAsync(int id);
        Task AddAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task SaveChangesAsync();
    }
}
