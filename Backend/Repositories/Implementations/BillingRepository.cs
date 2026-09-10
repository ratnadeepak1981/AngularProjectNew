using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;

namespace CampusServicesPortal.Repositories.Implementations
{
    public class BillingRepository : IBillingRepository
    {
        private readonly AppDbContext _context;

        public BillingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FeePayment?> GetFeePaymentByIdAsync(int id)
        {
            return await _context.FeePayments
                .Include(f => f.FeeType)
                .Include(f => f.Student)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<FeePayment>> GetOutstandingFeesByStudentIdAsync(int studentId)
        {
            // Retrieves historical billing sheets including both Outstanding and Paid invoices for the student dashboard
            return await _context.FeePayments
                .Include(f => f.FeeType)
                .Where(f => f.StudentId == studentId)
                .OrderByDescending(f => f.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<FeePayment>> GetAllFeeAssignmentsAsync()
        {
            return await _context.FeePayments
                .Include(f => f.FeeType)
                .Include(f => f.Student)
                .OrderByDescending(f => f.Id)
                .ToListAsync();
        }


        public async Task<IEnumerable<Student>> GetStudentsByFacultyIdAsync(int facultyId)
        {
            // Used to retrieve target student groups for bulk faculty fee assignment runs [PDF: 0.1.15]
            return await _context.Students
                .Where(s => s.FacultyId == facultyId)
                .ToListAsync();
        }

        public async Task AddFeePaymentAsync(FeePayment payment)
        {
            await _context.FeePayments.AddAsync(payment);
        }

        public async Task AddFeePaymentsBulkAsync(IEnumerable<FeePayment> payments)
        {
            // Optimized high-performance range insertion block for group assignments [PDF: 0.1.15]
            await _context.FeePayments.AddRangeAsync(payments);
        }


        public async Task<bool> HasDuplicateUnpaidFeeAsync(int studentId, int feeTypeId, string billingPeriod)
        {
            return await _context.FeePayments
                .AnyAsync(f => f.StudentId == studentId &&
                               f.FeeTypeId == feeTypeId &&
                               f.BillingPeriod == billingPeriod &&
                               f.Status == "Outstanding");
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
