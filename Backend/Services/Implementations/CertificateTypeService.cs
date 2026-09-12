using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusServicesPortal.Repositories;
using CampusServicesPortal.DTOs.Requests.MasterData;
using CampusServicesPortal.DTOs.Responses.MasterData;
using CampusServicesPortal.Models;
using CampusServicesPortal.Services.Interfaces;
using CampusServicesPortal.Wrappers;

namespace CampusServicesPortal.Services.Implementations
{
    public class CertificateTypeService : ICertificateTypeService
    {
        private readonly ICertificateTypeRepository _repository;

        public CertificateTypeService(ICertificateTypeRepository repository)
        {
            _repository = repository;
        }

        public async Task<ServiceResult<IEnumerable<CertificateTypeResponseDto>>> GetAllCertificateTypesAsync()
        {
            var items = await _repository.GetAllAsync();
            var response = items.Select(c => new CertificateTypeResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive
            });
            return ServiceResult<IEnumerable<CertificateTypeResponseDto>>.Success(response, 200);
        }

        public async Task<ServiceResult<CertificateTypeResponseDto>> CreateCertificateTypeAsync(CreateCertificateTypeRequestDto request)
        {
            if (await _repository.ExistsByNameAsync(request.Name))
                return ServiceResult<CertificateTypeResponseDto>.Failure("A certificate type with this title already exists.", 409);

            var item = new CertificateType
            {
                Name = request.Name,
                IsActive = true
            };

            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();

            var response = new CertificateTypeResponseDto { Id = item.Id, Name = item.Name, IsActive = item.IsActive };
            return ServiceResult<CertificateTypeResponseDto>.Success(response, 201);
        }

        public async Task<ServiceResult<CertificateTypeResponseDto>> UpdateCertificateTypeAsync(int id, UpdateCertificateTypeRequestDto request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return ServiceResult<CertificateTypeResponseDto>.Failure("Target certificate type record not found.", 404);

            if (await _repository.ExistsByNameAsync(request.Name, id))
                return ServiceResult<CertificateTypeResponseDto>.Failure("A certificate type with this title already exists.", 409);

            item.Name = request.Name;
            item.IsActive = request.IsActive;

            await _repository.UpdateAsync(item);
            await _repository.SaveChangesAsync();

            var response = new CertificateTypeResponseDto { Id = item.Id, Name = item.Name, IsActive = item.IsActive };
            return ServiceResult<CertificateTypeResponseDto>.Success(response, 200);
        }

        public async Task<ServiceResult<object>> DeleteCertificateTypeAsync(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
                return ServiceResult<object>.Failure("Target certificate type record not found.", 404);

            item.IsActive = false; // Soft-delete rule execution [PDF: 0.1.17]
            await _repository.UpdateAsync(item);
            await _repository.SaveChangesAsync();

            return ServiceResult<object>.Success(new { Message = "Certificate type successfully deactivated." }, 200);
        }
    }
}
