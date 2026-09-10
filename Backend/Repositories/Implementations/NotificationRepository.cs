using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Notification>> GetByStudentIdAsync(int studentId)
        {
            // Business Rule: Ensure collection tracks newest records first [PDF: 0.1.16]
            return await _context.Notifications
                .AsNoTracking()
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetAllAsync()
        {
            return await _context.Notifications
                .AsNoTracking()
                .OrderByDescending(n => n.Id) // Sorts system audit ledger latest logs first
                .ToListAsync();
        }

        public async Task<IEnumerable<NotificationWithStudent>> GetAllWithStudentDetailsAsync()
        {
            return await _context.Notifications
                .AsNoTracking()
                .Join(
                    _context.Students,
                    notification => notification.StudentId, // Matches StudentId in Notifications
                    student => student.Id,                  // 🌟 CHANGED: Links directly to s.Id just like your working SQL!
                    (notification, student) => new NotificationWithStudent
                    {
                        Id = notification.Id,
                        IndexNumber = student.IndexNumber, // Pulls the index string perfectly for both students
                        Type = notification.Type,
                        Message = notification.Message,
                        IsRead = notification.IsRead,
                        CreatedAt = notification.CreatedAt
                    }
                )
                .OrderByDescending(n => n.Id) // Orders by latest logs first
                .ToListAsync();
        }


        public async Task<Notification?> GetByIdAndStudentIdAsync(int id, int studentId)
        {
            // Security Constraint: Ensures a student can only retrieve owned notifications [PDF: 0.1.16]
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.StudentId == studentId);
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
