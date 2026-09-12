using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories
{
    public class CertificateTypeRepository : ICertificateTypeRepository
    {
        private readonly AppDbContext _context;

        public CertificateTypeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CertificateType>> GetAllAsync()
        {
            return await _context.CertificateTypes.AsNoTracking().ToListAsync();
        }

        public async Task<CertificateType?> GetByIdAsync(int id)
        {
            return await _context.CertificateTypes.FindAsync(id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var query = _context.CertificateTypes.AsQueryable();
            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);
            return await query.AnyAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> HasLinkedRequestsAsync(int certificateTypeId)
        {
            // Business Rule: Check if an active certificate request references this lookup entity [PDF: 0.1.17]
            return await _context.CertificateRequests.AnyAsync(r => r.CertificateTypeId == certificateTypeId);
        }

        public async Task AddAsync(CertificateType certificateType)
        {
            await _context.CertificateTypes.AddAsync(certificateType);
        }

        public async Task UpdateAsync(CertificateType certificateType)
        {
            _context.CertificateTypes.Update(certificateType);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


    }
}
