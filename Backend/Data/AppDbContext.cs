
using Microsoft.EntityFrameworkCore;
using CampusServicesPortal.Models;

namespace CampusServicesPortal.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ============================================================
        // MODULE 1: Student Profile & Authentication
        // ============================================================

        public DbSet<User> Users => Set<User>();
        public DbSet<StudentMasterList> StudentMasterLists => Set<StudentMasterList>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<StudentPhoneNumber> StudentPhoneNumbers => Set<StudentPhoneNumber>();
        public DbSet<StudentAddress> StudentAddresses => Set<StudentAddress>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        // ============================================================
        // MODULE 2: Hostel Accommodation
        // ============================================================

        public DbSet<Hostel> Hostels => Set<Hostel>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<HostelApplication> HostelApplications => Set<HostelApplication>();

        // ============================================================
        // MODULE 3: Lab Reservation
        // ============================================================

        public DbSet<Lab> Labs => Set<Lab>();
        public DbSet<LabSeat> LabSeats => Set<LabSeat>();
        public DbSet<LabBooking> LabBookings => Set<LabBooking>();

        // ============================================================
        // MODULE 4: Event Registration
        // ============================================================

        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();

        // ============================================================
        // MODULE 5: Complaint Management
        // ============================================================

        public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
        public DbSet<Complaint> Complaints => Set<Complaint>();

        // ============================================================
        // MODULE 6: Certificate Requests
        // ============================================================

        public DbSet<CertificateRequest> CertificateRequests => Set<CertificateRequest>();

        // ============================================================
        // MODULE 7: Fee Payment Simulation
        // ============================================================

        public DbSet<FeeType> FeeTypes => Set<FeeType>();
        public DbSet<FeePayment> FeePayments => Set<FeePayment>();

        // ============================================================
        // MODULE 8: Notifications
        // ============================================================

        public DbSet<Notification> Notifications => Set<Notification>();

        // ============================================================
        // MODULE 9: Master Data & Settings
        // ============================================================

        public DbSet<Faculty> Faculties => Set<Faculty>();
        public DbSet<CertificateType> CertificateTypes => Set<CertificateType>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

        // ============================================================
        // MODULE 10: Audit Log Trail
        // ============================================================

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================================================
            // MODULE 1: STUDENT PROFILE & AUTHENTICATION
            // ========================================================

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<StudentMasterList>()
                .HasIndex(s => s.IndexNumber)
                .IsUnique();

            // StudentMasterList -> Faculty
            modelBuilder.Entity<StudentMasterList>()
                .HasOne<Faculty>()
                .WithMany()
                .HasForeignKey(s => s.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Student -> User
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student -> Faculty
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Faculty)
                .WithMany()
                .HasForeignKey(s => s.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Student -> Phone Numbers
            modelBuilder.Entity<StudentPhoneNumber>()
                .HasOne(p => p.Student)
                .WithMany(s => s.PhoneNumbers)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student -> Addresses
            modelBuilder.Entity<StudentAddress>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Addresses)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student -> Password Reset Tokens
            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Refresh Tokens
            modelBuilder.Entity<RefreshToken>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========================================================
            // MODULE 2: HOSTEL ACCOMMODATION
            // ========================================================

            modelBuilder.Entity<Room>()
                .HasOne(r => r.Hostel)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HostelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HostelApplication>()
                .HasOne(ha => ha.Student)
                .WithMany()
                .HasForeignKey(ha => ha.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HostelApplication>()
                .HasOne(ha => ha.PreferredHostel)
                .WithMany()
                .HasForeignKey(ha => ha.PreferredHostelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HostelApplication>()
                .HasOne(ha => ha.AssignedRoom)
                .WithMany()
                .HasForeignKey(ha => ha.AssignedRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HostelApplication>()
                .Property(ha => ha.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ========================================================
            // MODULE 3: LAB RESERVATION
            // ========================================================

            modelBuilder.Entity<LabSeat>()
                .HasOne(ls => ls.Lab)
                .WithMany(l => l.Seats)
                .HasForeignKey(ls => ls.LabId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LabBooking>()
                .HasOne(lb => lb.Lab)
                .WithMany()
                .HasForeignKey(lb => lb.LabId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LabBooking>()
                .HasOne(lb => lb.Student)
                .WithMany()
                .HasForeignKey(lb => lb.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LabBooking>()
                .HasOne(lb => lb.Seat)
                .WithMany()
                .HasForeignKey(lb => lb.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate confirmed booking for the same student,
            // date and time slot.
            modelBuilder.Entity<LabBooking>()
                .HasIndex(lb => new
                {
                    lb.StudentId,
                    lb.BookingDate,
                    lb.TimeSlot
                })
                .HasDatabaseName("UX_LabBookings_Student_ActiveSlot")
                .HasFilter("[Status] = 'Confirmed'")
                .IsUnique();

            // Prevent two students from booking the same seat,
            // date and time slot.
            modelBuilder.Entity<LabBooking>()
                .HasIndex(lb => new
                {
                    lb.SeatId,
                    lb.BookingDate,
                    lb.TimeSlot
                })
                .HasDatabaseName("UX_LabBookings_Seat_ActiveSlot")
                .HasFilter("[Status] = 'Confirmed'")
                .IsUnique();

            // ========================================================
            // MODULE 4: EVENT REGISTRATION
            // ========================================================

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Venue)
                .WithMany()
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.Event)
                .WithMany()
                .HasForeignKey(er => er.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.Student)
                .WithMany()
                .HasForeignKey(er => er.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========================================================
            // MODULE 5: COMPLAINT MANAGEMENT
            // ========================================================

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.Category)
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate complaint category names.
            modelBuilder.Entity<ComplaintCategory>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // ========================================================
            // MODULE 6: CERTIFICATE REQUESTS
            // ========================================================

            modelBuilder.Entity<CertificateRequest>()
                .HasOne(cr => cr.Student)
                .WithMany()
                .HasForeignKey(cr => cr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CertificateRequest>()
                .HasOne(cr => cr.CertificateType)
                .WithMany()
                .HasForeignKey(cr => cr.CertificateTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate certificate type master records.
            modelBuilder.Entity<CertificateType>()
                .HasIndex(ct => ct.Name)
                .IsUnique();

            // ========================================================
            // MODULE 7: FEE PAYMENTS
            // ========================================================

            modelBuilder.Entity<FeePayment>()
                .Property(fp => fp.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<FeePayment>()
                .HasOne(fp => fp.Student)
                .WithMany()
                .HasForeignKey(fp => fp.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FeePayment>()
                .HasOne(fp => fp.FeeType)
                .WithMany()
                .HasForeignKey(fp => fp.FeeTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========================================================
            // MODULE 8: NOTIFICATIONS
            // ========================================================

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Student)
                .WithMany()
                .HasForeignKey(n => n.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // ========================================================
            // MODULE 9: MASTER DATA & SETTINGS
            // ========================================================

            modelBuilder.Entity<SystemSetting>()
                .HasKey(s => s.SettingKey);

            // ========================================================
            // MODULE 10: AUDIT LOG
            // ========================================================

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new
                {
                    a.Module,
                    a.Action
                });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new
                {
                    a.IsReviewed,
                    a.IsSuccess
                });

            // ========================================================
            // SEED DATA
            // ========================================================

            // System Settings
            modelBuilder.Entity<SystemSetting>().HasData(
                new SystemSetting
                {
                    SettingKey = "LabBookingHoldMinutes",
                    SettingValue = "15"
                },
                new SystemSetting
                {
                    SettingKey = "MaxDailyLabBookings",
                    SettingValue = "1"
                }
            );

            // Faculties
            modelBuilder.Entity<Faculty>().HasData(
                new Faculty
                {
                    Id = 1,
                    Name = "Faculty of Computing",
                    IsActive = true
                },
                new Faculty
                {
                    Id = 2,
                    Name = "Faculty of Engineering",
                    IsActive = true
                },
                new Faculty
                {
                    Id = 3,
                    Name = "Faculty of Business Management",
                    IsActive = true
                }
            );

            // Fee Types
            modelBuilder.Entity<FeeType>().HasData(
                new FeeType
                {
                    Id = 1,
                    Name = "Tuition Fee",
                    IsActive = true
                },
                new FeeType
                {
                    Id = 2,
                    Name = "Lab Fine / Equipment Fee",
                    IsActive = true
                },
                new FeeType
                {
                    Id = 3,
                    Name = "Hostel Accommodation Fee",
                    IsActive = true
                },
                new FeeType
                {
                    Id = 4,
                    Name = "Library Fine & Late Return",
                    IsActive = true
                },
                new FeeType
                {
                    Id = 5,
                    Name = "Student Identity Card Renewal Fee",
                    IsActive = true
                }
            );

            // Complaint Categories
            modelBuilder.Entity<ComplaintCategory>().HasData(
                new ComplaintCategory
                {
                    Id = 1,
                    Name = "Hostel Room Maintenance",
                    IsActive = true
                },
                new ComplaintCategory
                {
                    Id = 2,
                    Name = "Network WiFi Interruption",
                    IsActive = true
                }
            );

            // Certificate Types
            modelBuilder.Entity<CertificateType>().HasData(
                new CertificateType
                {
                    Id = 1,
                    Name = "Official Academic Transcript",
                    IsActive = true
                },
                new CertificateType
                {
                    Id = 2,
                    Name = "Bonafide Student Status Letter",
                    IsActive = true
                }
            );

            // NOTE:
            // HostelApplication seed data intentionally removed.
            // The previous seed referenced Hostels Id 1 and 2 and
            // Room Id 1, which do not currently exist in the database.
            //
            // Hostel applications should be created through the
            // Hostel module after valid Hostels, Rooms and Students
            // exist.
        }
    }
}

