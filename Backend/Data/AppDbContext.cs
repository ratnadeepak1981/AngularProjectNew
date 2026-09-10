
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
        public DbSet<LabBookingTimeSlot> LabBookingTimeSlots => Set<LabBookingTimeSlot>();

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
            // TEMPORARY BYPASS: DO NOT CREATE TABLES (ALREADY IN DB)
            // ========================================================
            /*
            modelBuilder.Entity<User>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<StudentMasterList>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Student>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<StudentPhoneNumber>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<StudentAddress>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<PasswordResetToken>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<PasswordHistory>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<RefreshToken>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Hostel>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Room>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<HostelApplication>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Lab>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<LabSeat>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<LabBooking>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<LabBookingTimeSlot>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Venue>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Event>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<EventRegistration>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<ComplaintCategory>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Complaint>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<CertificateRequest>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<FeeType>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<FeePayment>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Notification>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Faculty>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<CertificateType>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<SystemSetting>().ToTable(t => t.ExcludeFromMigrations());
            modelBuilder.Entity<AuditLog>().ToTable(t => t.ExcludeFromMigrations());
            */
            // ... The rest of your existing configurations follow here ...



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

            modelBuilder.Entity<LabBookingTimeSlot>()
                .HasOne(ts => ts.Lab)
                .WithMany(l => l.TimeSlots)
                .HasForeignKey(ts => ts.LabId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LabBookingTimeSlot>()
                .HasIndex(ts => new { ts.LabId, ts.StartTime, ts.EndTime })
                .HasDatabaseName("UX_LabBookingTimeSlots_Lab_TimeRange")
                .IsUnique();

            modelBuilder.Entity<LabBooking>()
                .HasOne(lb => lb.BookingTimeSlot)
                .WithMany()
                .HasForeignKey(lb => lb.TimeSlotId)
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
                    SettingKey = "LabBookingSlotDurationMinutes",
                    SettingValue = "15"
                },
                new SystemSetting
                {
                    SettingKey = "MaxLabBookingsPerStudentPerDay",
                    SettingValue = "2"
                },
                new SystemSetting
                {
                    SettingKey = "MaxDailySlots",
                    SettingValue = "2"
                }
            );

            // 1. Seed the parent Labs First (with required LabType property included)
            modelBuilder.Entity<Lab>().HasData(
                new Lab { Id = 1, Name = "Computer Lab 1", IsActive = true, LabType = "Computer" },
                new Lab { Id = 2, Name = "Computer Lab 2", IsActive = true, LabType = "Computer" }
            );

            // 2. Sample Seed Data for Lab Booking Time Slots
            modelBuilder.Entity<LabBookingTimeSlot>().HasData(
                new LabBookingTimeSlot { Id = 1, LabId = 1, StartTime = "09:00", EndTime = "11:00", DisplayOrder = 1, IsActive = true },
                new LabBookingTimeSlot { Id = 2, LabId = 1, StartTime = "11:00", EndTime = "13:00", DisplayOrder = 2, IsActive = true },
                new LabBookingTimeSlot { Id = 3, LabId = 1, StartTime = "14:00", EndTime = "16:00", DisplayOrder = 3, IsActive = true },
                new LabBookingTimeSlot { Id = 4, LabId = 1, StartTime = "16:00", EndTime = "18:00", DisplayOrder = 4, IsActive = true },

                new LabBookingTimeSlot { Id = 5, LabId = 2, StartTime = "09:00", EndTime = "11:00", DisplayOrder = 1, IsActive = true },
                new LabBookingTimeSlot { Id = 6, LabId = 2, StartTime = "11:00", EndTime = "13:00", DisplayOrder = 2, IsActive = true },
                new LabBookingTimeSlot { Id = 7, LabId = 2, StartTime = "14:00", EndTime = "16:00", DisplayOrder = 3, IsActive = true },
                new LabBookingTimeSlot { Id = 8, LabId = 2, StartTime = "16:00", EndTime = "18:00", DisplayOrder = 4, IsActive = true }
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
            

            // ========================================================================
            // 🌟 ARCHITECTURAL SECURITY UPDATE: SYSTEM-WIDE UNIQUENESS CONSTRAINTS
            // ========================================================================

            // 🔒 Enforce Unique Master Data Names (Module 4 & Module 2 & Module 3)
            modelBuilder.Entity<Venue>()
                .HasIndex(v => v.Name)
                .HasDatabaseName("IX_Venues_Name")
                .IsUnique();

            modelBuilder.Entity<Hostel>()
                .HasIndex(h => h.Name)
                .HasDatabaseName("IX_Hostels_Name")
                .IsUnique();

            modelBuilder.Entity<Lab>()
                .HasIndex(l => l.Name)
                .HasDatabaseName("IX_Labs_Name")
                .IsUnique();

            // 🔒 Enforce Unique Contact Communications (Module 1)
            modelBuilder.Entity<StudentPhoneNumber>()
                .HasIndex(p => p.PhoneNumber) // 🌟 FIXED: Changed from 'Value' to 'PhoneNumber'
                .HasDatabaseName("IX_StudentPhoneNumbers_PhoneNumber")
                .IsUnique();

            // 🔒 Enforce Unique Financial Configurations (Module 7)
            modelBuilder.Entity<FeeType>()
                .HasIndex(ft => ft.Name)
                .HasDatabaseName("IX_FeeTypes_Name")
                .IsUnique();

            // 🔒 COMPOSITE SCHEDULING CONSTRAINT (Module 4)
            modelBuilder.Entity<Event>()
                .HasIndex(e => new { e.VenueId, e.StartDateTime })
                .HasDatabaseName("UX_Events_Venue_Schedule")
                .IsUnique();
            modelBuilder.Entity<Room>()
                .HasIndex(r => new { r.HostelId, r.RoomNumber }) // Adjust property name to 'Number' if your model uses that name
                .HasDatabaseName("UX_Rooms_Hostel_RoomNumber")
                .IsUnique();

            // ========================================================================
            // 🔒 COMPREHENSIVE REPAIR: ADDITIONAL PROFILE CONSTRAINTS (Module 1)
            // ========================================================================

            // Enforce that active student profile index strings remain completely distinct
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.IndexNumber)
                .HasDatabaseName("IX_Students_IndexNumber")
                .IsUnique();

            // Enforce that active student contact strings cannot be duplicated across rows
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.ContactDetails)
                .HasDatabaseName("IX_Students_ContactDetails")
                .IsUnique();


        }
    }
}

