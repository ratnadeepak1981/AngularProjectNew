using CampusServicesPortal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusServicesPortal.Application.Interfaces.Repositories
{
    public interface IFacultyRepository
    {
        Task<IEnumerable<Faculty>> GetAllAsync();
        Task<Faculty?> GetByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> HasLinkedStudentsAsync(int facultyId);
        Task AddAsync(Faculty faculty);
        Task UpdateAsync(Faculty faculty);
        Task SaveChangesAsync();
    }
}
