using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories.Interfaces
{
    public interface IComplaintRepository
    {
        Task<bool> StudentExistsAsync(int studentId);

        Task<Complaint?> GetComplaintByIdAsync(int id);

        Task<IEnumerable<Complaint>> GetComplaintsByStudentIdAsync(int studentId);
        Task<IEnumerable<Complaint>> GetComplaintsAsync(string? status);

        Task<bool> HasDuplicatePendingComplaintAsync(int studentId, int categoryId, string description);

        Task AddComplaintAsync(Complaint complaint);

        Task<ComplaintCategory?> GetCategoryByIdAsync(int categoryId);

        Task<IEnumerable<ComplaintCategory>> GetActiveCategoriesAsync();

        Task<bool> CategoryNameExistsAsync(string name, int? excludeCategoryId = null);

        Task<bool> CategoryHasComplaintsAsync(int categoryId);

        Task AddCategoryAsync(ComplaintCategory category);

        void UpdateCategory(ComplaintCategory category);

        void DeleteCategory(ComplaintCategory category);

        Task SaveChangesAsync();
    }
}
