using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusServicesPortal.Repositories.Implementations
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly AppDbContext _context;

        public ComplaintRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> StudentExistsAsync(int studentId)
        {
            return await _context.Students
                .AnyAsync(s => s.Id == studentId);
        }

        public async Task<Complaint?> GetComplaintByIdAsync(int id)
        {
            return await ComplaintQuery()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Complaint>>
            GetComplaintsByStudentIdAsync(int studentId)
        {
            return await ComplaintQuery()
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Complaint>>
            GetComplaintsAsync(string? status)
        {
            var query = ComplaintQuery();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasDuplicatePendingComplaintAsync(int studentId, int categoryId, string description)
        {
            string cleanDesc = description.Trim().ToLower();
            return await _context.Complaints
                .AnyAsync(c => c.StudentId == studentId
                            && c.CategoryId == categoryId
                            && c.Status == "Pending"
                            && c.Description.ToLower() == cleanDesc);
        }

        public async Task AddComplaintAsync(Complaint complaint)
        {
            await _context.Complaints.AddAsync(complaint);
        }

        public async Task<ComplaintCategory?>
            GetCategoryByIdAsync(int categoryId)
        {
            return await _context.ComplaintCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId);
        }

        public async Task<IEnumerable<ComplaintCategory>>
            GetActiveCategoriesAsync()
        {
            return await _context.ComplaintCategories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<bool> CategoryNameExistsAsync(
            string name,
            int? excludeCategoryId = null)
        {
            var normalizedName = name.Trim().ToLower();

            var query = _context.ComplaintCategories
                .Where(c => c.Name.ToLower() == normalizedName);

            if (excludeCategoryId.HasValue)
            {
                query = query.Where(
                    c => c.Id != excludeCategoryId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool>
            CategoryHasComplaintsAsync(int categoryId)
        {
            return await _context.Complaints
                .AnyAsync(c => c.CategoryId == categoryId);
        }

        public async Task
            AddCategoryAsync(ComplaintCategory category)
        {
            await _context.ComplaintCategories
                .AddAsync(category);
        }

        public void UpdateCategory(ComplaintCategory category)
        {
            _context.ComplaintCategories.Update(category);
        }

        public void DeleteCategory(ComplaintCategory category)
        {
            _context.ComplaintCategories.Remove(category);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private IQueryable<Complaint> ComplaintQuery()
        {
            return _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.Category);
        }
    }
}