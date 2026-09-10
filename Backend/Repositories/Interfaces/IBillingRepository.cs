using System.Collections.Generic;
using System.Threading.Tasks;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories.Interfaces
{
    public interface IBillingRepository
    {
        Task<FeePayment?> GetFeePaymentByIdAsync(int id);

        Task<IEnumerable<FeePayment>> GetAllFeeAssignmentsAsync();

        Task<IEnumerable<FeePayment>> GetOutstandingFeesByStudentIdAsync(int studentId);
        Task<IEnumerable<Student>> GetStudentsByFacultyIdAsync(int facultyId);
        Task AddFeePaymentAsync(FeePayment payment);
        Task AddFeePaymentsBulkAsync(IEnumerable<FeePayment> payments);
        Task SaveChangesAsync();

        Task<bool> HasDuplicateUnpaidFeeAsync(int studentId, int feeTypeId, string billingPeriod);

    }
}
