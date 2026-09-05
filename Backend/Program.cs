using CampusServicesPortal.Application.Interfaces.Repositories;
using CampusServicesPortal.Data;
using CampusServicesPortal.Data.Seeding;
using CampusServicesPortal.Infrastructure.Repositories;
using CampusServicesPortal.Interceptors;
using CampusServicesPortal.Repositories;
using CampusServicesPortal.Repositories.Implementations;
using CampusServicesPortal.Repositories.Interfaces;
using CampusServicesPortal.Services;
using CampusServicesPortal.Services.Implementations;
using CampusServicesPortal.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace CampusServicesPortal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Temporary hash generator tool:
            var testHash = BCrypt.Net.BCrypt.HashPassword("123");
            Console.WriteLine($"YOUR_GENERATED_HASH_IS: {testHash}");

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddMemoryCache();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });


            // 🛠️ Configured: Rich Swagger UI with active Bearer Token Security Locking [PDF: 0.1.20]
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Campus Services Portal API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<AuditSaveChangesInterceptor>();

            builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var interceptor = serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>();
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("CampusServicesPortalConnection"),
                    sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .AddInterceptors(interceptor);
            });

            // Core Identity Service & Repository Registrations
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddScoped<IPasswordRepository, PasswordRepository>();
            builder.Services.AddScoped<IPasswordService, PasswordService>();

            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<IAccountService, AccountService>();

            // Module 1: Student Records Registrations
            builder.Services.AddScoped<IStudentRepository, StudentRepository>();
            builder.Services.AddScoped<IStudentService, StudentService>();




            // Module 2: Hostel Administrative Management Registrations
            builder.Services.AddScoped<IHostelManagementRepository, HostelManagementRepository>();
            builder.Services.AddScoped<IHostelManagementService, HostelManagementService>();

            // Module 2: Hostel Management Registrations [PDF: 0.1.6]

            builder.Services.AddScoped<IHostelRepository, HostelRepository>();
            builder.Services.AddScoped<IHostelService, HostelService>();

          
           // Module 3: Lab Reservations Registrations [PDF: 0.1.8]
            builder.Services.AddScoped<ILabRepository, LabRepository>();
            builder.Services.AddScoped<ILabBookingRepository, LabBookingRepository>();

            // --- Register your Split Services ---
            builder.Services.AddScoped<ILabService, LabService>();
            builder.Services.AddScoped<ILabBookingService, LabBookingService>(); // <-- FIX: Add this line!

            // --- Register the Background Daemon Worker Job ---
            builder.Services.AddHostedService<BookingExpiryWorker>();


            // Module 4: Event & Venue Scheduling Registrations [PDF: 0.1.9]
            builder.Services.AddScoped<IVenueRepository, VenueRepository>();
            builder.Services.AddScoped<IVenueService, VenueService>();
            builder.Services.AddScoped<IEventRepository, EventRepository>();
            builder.Services.AddScoped<IEventService, EventService>();

            // Module 7: Fees and Collections Billing Registrations [PDF: 0.1.14]
            builder.Services.AddScoped<IFeeTypeRepository, FeeTypeRepository>();
            builder.Services.AddScoped<IFeeTypeService, FeeTypeService>();
            builder.Services.AddScoped<IBillingRepository, BillingRepository>();
            builder.Services.AddScoped<IBillingService, BillingService>();


            // Register Faculty Module Mappings
            builder.Services.AddScoped<IFacultyRepository, FacultyRepository>();
            builder.Services.AddScoped<IFacultyService, FacultyService>();

            // Register Certificate Type Module Mappings [INDEX: 0.1.17]
            builder.Services.AddScoped<ICertificateTypeRepository, CertificateTypeRepository>();
            builder.Services.AddScoped<ICertificateTypeService, CertificateTypeService>();


            // Register Module 8: Notifications Engine Dependency Components
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<ISmsService, SmsService>();


            // Module 5: Complaint Management
            builder.Services.AddScoped<
                CampusServicesPortal.Repositories.Interfaces.IComplaintRepository,
                CampusServicesPortal.Repositories.Implementations.ComplaintRepository>();

            builder.Services.AddScoped<
                CampusServicesPortal.Services.Interfaces.IComplaintService,
                CampusServicesPortal.Services.Implementations.ComplaintService>();

            // Module 6: Certificate Requests
            builder.Services.AddScoped<
                CampusServicesPortal.Repositories.Interfaces.ICertificateRepository,
                CampusServicesPortal.Repositories.Implementations.CertificateRepository>();

            builder.Services.AddScoped<
                CampusServicesPortal.Services.Interfaces.ICertificateService,
                CampusServicesPortal.Services.Implementations.CertificateService>();

            // Module 10: Audit Log Trail
            builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();

            // Reports & Institutional Analytics Module
            builder.Services.AddScoped<
                CampusServicesPortal.Repositories.Interfaces.IReportRepository,
                CampusServicesPortal.Repositories.Implementations.ReportRepository>();
            builder.Services.AddScoped<
                CampusServicesPortal.Services.Interfaces.IReportService,
                CampusServicesPortal.Services.Implementations.ReportService>();

            // Module 3 & 4 Hold Sweeper Daemon Worker [PDF: 0.1.12, 0.1.19]
            builder.Services.AddHostedService<BookingExpiryWorker>();


            // 🛠️ Staging Gate: Uncomment this line later to activate global crash interceptor track

            // app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Configure Token Verification Parameters [PDF: 0.1.2, 0.1.18]
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"], // 🛠️ Fixed to read from appsettings
                    ValidAudience = builder.Configuration["Jwt:Audience"], // 🛠️ Fixed to read from appsettings
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])) // 🛠️ Dynamic key matching AuthService
                };
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            // Enable the policy globally
            app.UseCors("AllowAll");

            var frontendPath = Path.Combine(builder.Environment.ContentRootPath, "frontend");
            if (Directory.Exists(frontendPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath),
                    RequestPath = ""
                });
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();

            // 🛠️ Fixed Order: UseAuthentication MUST be configured ahead of UseAuthorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Seed initial Audit Log trail if empty
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    AuditLogDataSeeder.SeedAuditLogsAsync(context).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Error occurred during initial database seeding.");
                }
            }

            app.Run();
        }
    }
}
