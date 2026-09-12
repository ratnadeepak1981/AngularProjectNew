using System;
using System.Linq;
using System.Threading.Tasks;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusServicesPortal.Repositories.Implementations
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Student?> GetStudentByVerificationTokenAsync(string token)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(s => s.EmailVerificationToken == token);
        }

        public async Task<Student?> GetStudentByEmailThroughUserAsync(string email)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(s => s.User.Email == email);
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task UpdateStudentAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasActiveHostelRoomAsync(int studentId)
        {
            return await _context.HostelApplications
                .AnyAsync(h => h.StudentId == studentId && (h.Status == "Approved" || h.Status == "Room Assigned" || h.Status == "RoomAssigned"));
        }

        public async Task<bool> HasUpcomingLabBookingsAsync(int studentId)
        {
            return await _context.LabBookings
                .AnyAsync(l => l.StudentId == studentId && l.BookingDate >= DateTime.UtcNow.Date && (l.Status == "Held" || l.Status == "Confirmed"));
        }

        public async Task<bool> HasUpcomingEventRegistrationsAsync(int studentId)
        {
            return await _context.EventRegistrations
                .Include(er => er.Event)
                .AnyAsync(er => er.StudentId == studentId && er.Event.StartDateTime >= DateTime.UtcNow && er.Status == "Confirmed");
        }

        public async Task<bool> HasPendingCertificateRequestsAsync(int studentId)
        {
            return await _context.CertificateRequests
                .AnyAsync(c => c.StudentId == studentId && c.Status == "Pending");
        }

        public async Task<bool> HasOutstandingFeesAsync(int studentId)
        {
            return await _context.FeePayments
                .AnyAsync(f => f.StudentId == studentId && (f.Status == "Outstanding" || f.Status == "Unpaid"));
        }

        public async Task DeactivateStudentAccountAsync(int studentId)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == studentId);
            if (student != null)
            {
                student.DeactivatedAt = DateTime.UtcNow;
                if (student.User != null)
                {
                    student.User.IsActive = false;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReactivateStudentAccountAsync(int studentId)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == studentId);
            if (student != null)
            {
                student.DeactivatedAt = null;
                if (student.User != null)
                {
                    student.User.IsActive = true;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
