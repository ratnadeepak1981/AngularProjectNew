using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Implementations
{
    public class FeeTypeService : IFeeTypeService
    {
        private readonly IFeeTypeRepository _feeTypeRepository;

        public FeeTypeService(IFeeTypeRepository feeTypeRepository)
        {
            _feeTypeRepository = feeTypeRepository;
        }

        public async Task<ServiceResult<IEnumerable<FeeType>>> GetFeeTypesAsync()
        {
            var list = await _feeTypeRepository.GetActiveFeeTypesAsync();
            return ServiceResult<IEnumerable<FeeType>>.Success(list, 200);
        }

        public async Task<ServiceResult<FeeType>> CreateFeeTypeAsync(string name)
        {
            string cleanName = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
                return ServiceResult<FeeType>.Failure("Fee type name is required.", 400);

            if (await _feeTypeRepository.ExistsByNameAsync(cleanName))
                return ServiceResult<FeeType>.Failure($"A fee category named '{cleanName}' already exists.", 409);

            var feeType = new FeeType
            {
                Name = cleanName,
                IsActive = true
            };

            await _feeTypeRepository.AddFeeTypeAsync(feeType);
            await _feeTypeRepository.SaveChangesAsync();

            return ServiceResult<FeeType>.Success(feeType, 201);
        }

        public async Task<ServiceResult<FeeType>> UpdateFeeTypeAsync(int id, string name, bool isActive)
        {
            var feeType = await _feeTypeRepository.GetFeeTypeByIdAsync(id);
            if (feeType == null)
                return ServiceResult<FeeType>.Failure("Target fee type record not found.", 404);

            string cleanName = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cleanName))
                return ServiceResult<FeeType>.Failure("Fee type name is required.", 400);

            if (await _feeTypeRepository.ExistsByNameAsync(cleanName, id))
                return ServiceResult<FeeType>.Failure($"A fee category named '{cleanName}' already exists.", 409);

            feeType.Name = cleanName;
            feeType.IsActive = isActive;

            _feeTypeRepository.UpdateFeeType(feeType);
            await _feeTypeRepository.SaveChangesAsync();

            return ServiceResult<FeeType>.Success(feeType, 200);
        }

        public async Task<ServiceResult<FeeType>> ToggleFeeTypeStatusAsync(int id)
        {
            var feeType = await _feeTypeRepository.GetFeeTypeByIdAsync(id);
            if (feeType == null)
                return ServiceResult<FeeType>.Failure("Target fee type record not found.", 404);

            feeType.IsActive = !feeType.IsActive;
            _feeTypeRepository.UpdateFeeType(feeType);
            await _feeTypeRepository.SaveChangesAsync();

            return ServiceResult<FeeType>.Success(feeType, 200);
        }

        public async Task<ServiceResult<object>> DeleteFeeTypeAsync(int id)
        {
            var feeType = await _feeTypeRepository.GetFeeTypeByIdAsync(id);
            if (feeType == null)
                return ServiceResult<object>.Failure("Target fee type record not found.", 404);

            feeType.IsActive = false;
            _feeTypeRepository.UpdateFeeType(feeType);
            await _feeTypeRepository.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Fee type deactivated successfully." }, 200);
        }
    }
}
