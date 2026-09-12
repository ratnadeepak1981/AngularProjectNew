using CampusServicesPortal.Data;
using CampusServicesPortal.Models;
using CampusServicesPortal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusServicesPortal.Repositories.Implementations
{
    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;

        public StudentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<StudentMasterList?> GetMasterRecordAsync(string indexNumber)
        {
            return await _context.StudentMasterLists
                .FirstOrDefaultAsync(m => m.IndexNumber == indexNumber);
        }
        public async Task<IEnumerable<FeePayment>> GetAllFeeAssignmentsAsync()
        {
            return await _context.FeePayments
                .Include(f => f.FeeType)
                .Include(f => f.Student) // Includes student metadata so names show up on the admin grid
                .OrderByDescending(f => f.Id)
                .ToListAsync();
        }


        public async Task<bool> IsIndexRegisteredAsync(string indexNumber)
        {
            return await _context.Students
                .AnyAsync(s => s.IndexNumber == indexNumber);
        }

        public async Task<bool> IsEmailRegisteredAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task AddStudentAsync(Student student)
        {
            await _context.Students.AddAsync(student);
        }
        // PUT /api/students/{id} Tracking registration context state changes
        public Task UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            return Task.CompletedTask;
        }

        // GET /api/students Search/Filtering Logic [BRD Module 1]
        public async Task<IEnumerable<Student>> SearchAsync(string? search, string? faculty)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.FullName.Contains(search) || s.IndexNumber.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(faculty))
            {
                query = query.Where(s => s.Faculty.Name == faculty || s.FacultyId.ToString() == faculty);
            }

            return await query.ToListAsync();
        }


        // GET /api/students/{id} Lookup [Index 0.1.3]
        public async Task<Student?> GetByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .Include(s => s.PhoneNumbers)
                .Include(s => s.Addresses)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Student?> GetByIndexOrEmailAsync(string indexOrEmail)
        {
            var clean = (indexOrEmail ?? string.Empty).Trim().ToLower();
            return await _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .Include(s => s.PhoneNumbers)
                .Include(s => s.Addresses)
                .FirstOrDefaultAsync(s => s.IndexNumber.ToLower() == clean || (s.User != null && s.User.Email.ToLower() == clean));
        }

        // GET /api/students Search/Filtering Logic [Index 0.1.3]
        public async Task<IEnumerable<Student>> SearchStudentsAsync(string? search, string? faculty)
        {
            var query = _context.Students
                .Include(s => s.User)
                .Include(s => s.Faculty)
                .Include(s => s.PhoneNumbers)
                .Include(s => s.Addresses)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.FullName.Contains(search) || s.IndexNumber.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(faculty))
            {
                query = query.Where(s => s.Faculty.Name == faculty || s.FacultyId.ToString() == faculty);
            }

            return await query.ToListAsync();
        }

        public async Task SyncPhoneNumbersAsync(int studentId, IEnumerable<StudentPhoneNumber> phoneNumbers)
        {
            var existingPhones = await _context.StudentPhoneNumbers
                .Where(p => p.StudentId == studentId)
                .ToListAsync();

            var incomingList = phoneNumbers.ToList();
            var matchedExisting = new HashSet<int>();

            foreach (var incoming in incomingList)
            {
                StudentPhoneNumber? existing = null;
                if (incoming.Id > 0)
                {
                    existing = existingPhones.FirstOrDefault(e => e.Id == incoming.Id);
                }

                if (existing == null)
                {
                    existing = existingPhones
                        .FirstOrDefault(e => !matchedExisting.Contains(e.Id) &&
                                             string.Equals(e.PhoneType, incoming.PhoneType, StringComparison.OrdinalIgnoreCase));
                }

                if (existing != null)
                {
                    // In-place mutation -> EF Core marks entity as Modified with static Primary Key
                    matchedExisting.Add(existing.Id);
                    existing.PhoneType = incoming.PhoneType;
                    existing.PhoneNumber = incoming.PhoneNumber;
                    existing.IsPrimary = incoming.IsPrimary;
                    existing.IsVerified = incoming.IsVerified;
                }
                else
                {
                    // Brand new phone number
                    incoming.Id = 0;
                    incoming.StudentId = studentId;
                    await _context.StudentPhoneNumbers.AddAsync(incoming);
                }
            }

            var toRemove = existingPhones.Where(e => !matchedExisting.Contains(e.Id)).ToList();
            if (toRemove.Count > 0)
            {
                _context.StudentPhoneNumbers.RemoveRange(toRemove);
            }

            // Enforce single-primary integrity: only one number may have IsPrimary = true
            var allActivePhones = existingPhones.Where(e => matchedExisting.Contains(e.Id)).Concat(incomingList.Where(i => i.Id == 0)).ToList();
            var primaryItem = allActivePhones.FirstOrDefault(p => p.IsPrimary) ?? allActivePhones.FirstOrDefault();
            if (primaryItem != null)
            {
                foreach (var p in allActivePhones)
                {
                    p.IsPrimary = ReferenceEquals(p, primaryItem);
                }
            }
        }

        public async Task SyncAddressesAsync(int studentId, IEnumerable<StudentAddress> addresses)
        {
            var existingAddresses = await _context.StudentAddresses
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            var incomingList = addresses.ToList();
            var matchedExisting = new HashSet<int>();

            foreach (var incoming in incomingList)
            {
                StudentAddress? existing = null;
                if (incoming.Id > 0)
                {
                    existing = existingAddresses.FirstOrDefault(e => e.Id == incoming.Id);
                }

                if (existing == null)
                {
                    existing = existingAddresses
                        .FirstOrDefault(e => !matchedExisting.Contains(e.Id) &&
                                             string.Equals(e.AddressType, incoming.AddressType, StringComparison.OrdinalIgnoreCase));
                }

                if (existing == null && existingAddresses.Count > 0)
                {
                    existing = existingAddresses.FirstOrDefault(e => !matchedExisting.Contains(e.Id));
                }

                if (existing != null)
                {
                    // In-place mutation -> EF Core marks entity as Modified with static Primary Key
                    matchedExisting.Add(existing.Id);
                    existing.AddressType = incoming.AddressType;
                    existing.AddressLine1 = incoming.AddressLine1;
                    existing.AddressLine2 = incoming.AddressLine2;
                    existing.City = incoming.City;
                    existing.DistrictOrProvince = incoming.DistrictOrProvince;
                    existing.PostalCode = incoming.PostalCode;
                    existing.Country = incoming.Country;
                    existing.IsPrimary = incoming.IsPrimary;
                }
                else
                {
                    // Brand new address
                    incoming.Id = 0;
                    incoming.StudentId = studentId;
                    await _context.StudentAddresses.AddAsync(incoming);
                }
            }

            var toRemove = existingAddresses.Where(e => !matchedExisting.Contains(e.Id)).ToList();
            if (toRemove.Count > 0)
            {
                _context.StudentAddresses.RemoveRange(toRemove);
            }
        }

        public async Task<IEnumerable<StudentMasterList>> SearchMasterListAsync(string? search)
        {
            var query = _context.StudentMasterLists.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.FullName.Contains(search) || m.IndexNumber.Contains(search));
            }
            return await query.ToListAsync();
        }

        public async Task<HashSet<string>> GetRegisteredIndexNumbersAsync()
        {
            var registeredIndices = await _context.Students
                .Where(s => s.IndexNumber != null && s.IndexNumber != "")
                .Select(s => s.IndexNumber.ToLower())
                .ToListAsync();
            return new HashSet<string>(registeredIndices);
        }

        public async Task<int> BulkImportMasterListAsync(IEnumerable<StudentMasterList> masterRecords)
        {
            var rawIndices = await _context.StudentMasterLists
                .Select(m => m.IndexNumber)
                .ToListAsync();

            var validFacultyIds = new HashSet<int>(
                await _context.Faculties.Select(f => f.Id).ToListAsync()
            );

            var addedInBatch = new HashSet<string>(
                rawIndices
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => System.Text.RegularExpressions.Regex.Replace(s.Trim('"', '\'', ' ', '\t', '\r', '\n', '\uFEFF'), @"\s+", "").ToUpperInvariant()),
                StringComparer.OrdinalIgnoreCase
            );
            int newlyInsertedCount = 0;

            foreach (var record in masterRecords)
            {
                var rawIdx = record.IndexNumber?.Trim('"', '\'', ' ', '\t', '\r', '\n', '\uFEFF');
                if (string.IsNullOrWhiteSpace(rawIdx)) continue;

                var normalizedIdx = System.Text.RegularExpressions.Regex.Replace(rawIdx, @"\s+", "").ToUpperInvariant();

                if (!addedInBatch.Contains(normalizedIdx) && validFacultyIds.Contains(record.FacultyId))
                {
                    record.IndexNumber = rawIdx;
                    record.FullName = record.FullName?.Trim('"', '\'', ' ', '\t', '\r', '\n');
                    addedInBatch.Add(normalizedIdx);
                    await _context.StudentMasterLists.AddAsync(record);
                    newlyInsertedCount++;
                }
            }

            return newlyInsertedCount;
        }


        // Rule 11: Block if student has an active hostel allocation ('Approved' or 'Room Assigned') [Index 0.1.5, 0.1.19]
        public async Task<bool> HasActiveHostelAllocationAsync(int studentId)
        {
            return await _context.HostelApplications
                .AnyAsync(h => h.StudentId == studentId && (h.Status == "Approved" || h.Status == "Room Assigned"));
        }

        // Rule 11: Block if student has future-dated lab bookings ('Held' or 'Confirmed') [Index 0.1.5, 0.1.19]
        public async Task<bool> HasUpcomingLabBookingsAsync(int studentId)
        {
            return await _context.LabBookings
                .AnyAsync(l => l.StudentId == studentId && l.BookingDate >= DateTime.UtcNow && (l.Status == "Held" || l.Status == "Confirmed"));
        }

        // Rule 11: Block if student has upcoming future-dated event registrations [Index 0.1.5, 0.1.19]
        public async Task<bool> HasUpcomingEventRegistrationsAsync(int studentId)
        {
            return await _context.EventRegistrations
                .Include(er => er.Event)
                .AnyAsync(er => er.StudentId == studentId && er.Event.StartDateTime >= DateTime.UtcNow);
        }


        // Rule 11: Block if student has a pending certificate request [Index 0.1.5, 0.1.19]
        public async Task<bool> HasPendingCertificateRequestAsync(int studentId)
        {
            return await _context.CertificateRequests
                .AnyAsync(c => c.StudentId == studentId && c.Status == "Pending");
        }

        // Rule 11: Block if student has unpaid fees or outstanding library/lab fines [Index 0.1.5, 0.1.19]
        public async Task<bool> HasUnpaidFeesAsync(int studentId)
        {
            return await _context.FeePayments
                .AnyAsync(f => f.StudentId == studentId && f.Status == "Outstanding");
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
