using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories.Interfaces
{
    public interface IFeeTypeRepository
    {
        Task<IEnumerable<FeeType>> GetActiveFeeTypesAsync();
        Task<FeeType?> GetFeeTypeByIdAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeFeeTypeId = null);
        Task AddFeeTypeAsync(FeeType feeType);
        void UpdateFeeType(FeeType feeType);
        Task SaveChangesAsync();
    }
}
