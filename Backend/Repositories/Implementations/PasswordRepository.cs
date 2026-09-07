using System;
using System.Linq;
using System.Threading.Tasks;
using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusServicesPortal.Repositories.Implementations
{
    public class PasswordRepository : IPasswordRepository
    {
        private readonly AppDbContext _context;

        public PasswordRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Student?> GetStudentByUserIdAsync(int userId)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .Include(s => s.PhoneNumbers)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<Student?> GetStudentByEmailThroughUserAsync(string email)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .Include(s => s.PhoneNumbers)
                .FirstOrDefaultAsync(s => s.User.Email.ToLower() == email.Trim().ToLower());
        }

        public async Task InvalidateExistingResetTokensAsync(int studentId)
        {
            var tokens = await _context.PasswordResetTokens
                .Where(t => t.StudentId == studentId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsUsed = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task SavePasswordResetTokenAsync(PasswordResetToken token)
        {
            await _context.PasswordResetTokens.AddAsync(token);
            await _context.SaveChangesAsync();
        }

        public async Task<PasswordResetToken?> GetPasswordResetTokenAsync(string token)
        {
            return await _context.PasswordResetTokens
                .Include(p => p.Student)
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(p => p.Token == token && !p.IsUsed && p.ExpiresAt > DateTime.UtcNow);
        }


        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateResetTokenStatusAsync(PasswordResetToken token)
        {
            _context.PasswordResetTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllUserSessionsAsync(int userId)
        {
            var activeRefreshTokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && r.RevokedAt == null)
                .ToListAsync();

            foreach (var rt in activeRefreshTokens)
            {
                rt.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<SystemSetting?> GetSystemSettingAsync(string key)
        {
            return await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        }

        public async Task<IEnumerable<PasswordHistory>> GetRecentPasswordHistoriesAsync(int userId, int limit)
        {
            if (limit <= 0) return Enumerable.Empty<PasswordHistory>();
            return await _context.PasswordHistories
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task AddPasswordHistoryAsync(PasswordHistory history)
        {
            await _context.PasswordHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }
    }
}
