using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampusServicesPortal.DTOs.Requests.MasterData;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Controllers
{
    [Authorize]
    [Route("api/certificate-types")]
    public class CertificateTypesController : BaseApiController
    {
        private readonly ICertificateTypeService _certificateTypeService;

        public CertificateTypesController(ICertificateTypeService certificateTypeService)
        {
            _certificateTypeService = certificateTypeService;
        }

        // GET /api/certificate-types — List maintainable certificate types [PDF: 0.1.17]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _certificateTypeService.GetAllCertificateTypesAsync();
            return ProcessServiceResult(result, "Certificate types catalog retrieved successfully.");
        }

        // POST /api/certificate-types — Admin Only: Register a new certificate option [PDF: 0.1.18]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCertificateTypeRequestDto request)
        {
            var result = await _certificateTypeService.CreateCertificateTypeAsync(request);
            return ProcessServiceResult(result, "Certificate template option initialized successfully.");
        }

        // PUT /api/certificate-types/{id} — Admin Only: Update naming descriptors [PDF: 0.1.18]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCertificateTypeRequestDto request)
        {
            var result = await _certificateTypeService.UpdateCertificateTypeAsync(id, request);
            return ProcessServiceResult(result, "Certificate template properties modified successfully.");
        }

        // DELETE /api/certificate-types/{id} — Admin Only: Soft-deactivate if no active requests exist [PDF: 0.1.18]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _certificateTypeService.DeleteCertificateTypeAsync(id);
            return ProcessServiceResult(result, "Certificate template deactivation cycle finalized successfully.");
        }
    }
}
