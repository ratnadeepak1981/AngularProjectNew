using CampusServicesPortal.Application.Interfaces.Repositories;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusServicesPortal.Infrastructure.Repositories
{
    public class FacultyRepository : IFacultyRepository
    {
        private readonly AppDbContext _context;

        public FacultyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Faculty>> GetAllAsync()
        {
            return await _context.Faculties.AsNoTracking().ToListAsync();
        }

        public async Task<Faculty?> GetByIdAsync(int id)
        {
            return await _context.Faculties.FindAsync(id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            string cleanName = name.Trim().ToLower();
            var query = _context.Faculties.Where(f => f.Name.ToLower() == cleanName);
            if (excludeId.HasValue) query = query.Where(f => f.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<bool> HasLinkedStudentsAsync(int facultyId)
        {
            // Business Rule: Validate if student profile dependencies are anchored here [PDF: 0.1.17]
            return await _context.Students.AnyAsync(s => s.FacultyId == facultyId);
        }

        public async Task AddAsync(Faculty faculty)
        {
            await _context.Faculties.AddAsync(faculty);
        }

        public async Task UpdateAsync(Faculty faculty)
        {
            _context.Faculties.Update(faculty);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
