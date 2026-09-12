using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories
{
    public interface ICertificateTypeRepository
    {
        Task<IEnumerable<CertificateType>> GetAllAsync();
        Task<CertificateType?> GetByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> HasLinkedRequestsAsync(int certificateTypeId);
        Task AddAsync(CertificateType certificateType);
        Task UpdateAsync(CertificateType certificateType);
        Task SaveChangesAsync();
    }
}
