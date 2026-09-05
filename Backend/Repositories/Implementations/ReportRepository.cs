using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CampusServicesPortal.DTOs.Requests.Reports;
using CampusServicesPortal.DTOs.Responses.Reports;
using CampusServicesPortal.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CampusServicesPortal.Repositories.Implementations
{
    public class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;

        public ReportRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("CampusServicesPortalConnection")
                ?? throw new InvalidOperationException("Connection string 'CampusServicesPortalConnection' not found.");
        }

        private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

        // ==========================================
        // 1. INSTITUTIONAL KPI DASHBOARD (ADO.NET)
        // ==========================================
        public async Task<InstitutionalKpiReportDto> GetInstitutionalKpiSummaryAsync()
        {
            var kpi = new InstitutionalKpiReportDto();

            const string sql = @"
                -- 1. Student Profile & Intake
                SELECT 
                    COUNT(s.Id) AS TotalStudents,
                    SUM(CASE WHEN s.DeactivatedAt IS NULL AND s.EmailVerified = 1 THEN 1 ELSE 0 END) AS ActiveStudents,
                    SUM(CASE WHEN s.DeactivatedAt IS NOT NULL THEN 1 ELSE 0 END) AS DeactivatedStudents,
                    SUM(CASE WHEN s.EmailVerified = 0 THEN 1 ELSE 0 END) AS UnverifiedStudents
                FROM dbo.Students s;

                SELECT COUNT(1) AS MasterListTotal FROM dbo.StudentMasterLists;

                -- 2. Hostels & Beds
                SELECT 
                    COUNT(DISTINCT h.Id) AS TotalHostels,
                    ISNULL(SUM(r.MaxCapacity), 0) AS TotalBedCapacity
                FROM dbo.Hostels h
                LEFT JOIN dbo.Rooms r ON h.Id = r.HostelId AND r.IsActive = 1
                WHERE h.IsActive = 1;

                SELECT 
                    COUNT(DISTINCT ha.AssignedRoomId) AS AssignedRoomsCount,
                    COUNT(ha.Id) AS TotalAllocations
                FROM dbo.HostelApplications ha
                WHERE ha.Status = 'RoomAssigned' AND ha.AssignedRoomId IS NOT NULL;

                SELECT 
                    SUM(CASE WHEN ha.Status = 'Pending' THEN 1 ELSE 0 END) AS PendingApplications,
                    SUM(CASE WHEN ha.AssignedRoomId IS NULL THEN 1 ELSE 0 END) AS UnallocatedApplications
                FROM dbo.HostelApplications ha;

                -- 3. Lab Reservations
                SELECT COUNT(1) AS TotalLabs FROM dbo.Labs WHERE IsActive = 1;
                SELECT COUNT(1) AS TotalLabSeats FROM dbo.LabSeats WHERE IsBroken = 0;

                SELECT 
                    SUM(CASE WHEN lb.Status = 'Confirmed' THEN 1 ELSE 0 END) AS ConfirmedBookings,
                    SUM(CASE WHEN lb.Status = 'Held' AND lb.ExpiresAt > GETUTCDATE() THEN 1 ELSE 0 END) AS ActiveHolds,
                    SUM(CASE WHEN lb.Status IN ('Expired', 'Cancelled') OR (lb.Status = 'Held' AND lb.ExpiresAt <= GETUTCDATE()) THEN 1 ELSE 0 END) AS CancelledOrExpired
                FROM dbo.LabBookings lb;

                -- 4. Financial & Billing
                SELECT 
                    ISNULL(SUM(fp.Amount), 0) AS TotalBilled,
                    ISNULL(SUM(CASE WHEN fp.Status = 'Paid' THEN fp.Amount ELSE 0 END), 0) AS TotalCollected,
                    ISNULL(SUM(CASE WHEN fp.Status = 'Outstanding' THEN fp.Amount ELSE 0 END), 0) AS TotalOutstanding,
                    SUM(CASE WHEN fp.Status = 'Paid' THEN 1 ELSE 0 END) AS PaidInvoices,
                    SUM(CASE WHEN fp.Status = 'Outstanding' THEN 1 ELSE 0 END) AS UnpaidInvoices
                FROM dbo.FeePayments fp;

                -- 5. Complaints
                SELECT 
                    COUNT(c.Id) AS TotalComplaints,
                    SUM(CASE WHEN c.Status = 'Pending' THEN 1 ELSE 0 END) AS PendingComplaints,
                    SUM(CASE WHEN c.Status = 'In Progress' THEN 1 ELSE 0 END) AS InProgressComplaints,
                    SUM(CASE WHEN c.Status = 'Resolved' THEN 1 ELSE 0 END) AS ResolvedComplaints
                FROM dbo.Complaints c;

                -- 6. Certificates
                SELECT 
                    COUNT(cr.Id) AS TotalCertificates,
                    SUM(CASE WHEN cr.Status = 'Pending' THEN 1 ELSE 0 END) AS PendingCertificates,
                    SUM(CASE WHEN cr.Status = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCertificates,
                    SUM(CASE WHEN cr.Status = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCertificates
                FROM dbo.CertificateRequests cr;

                -- 7. Events
                SELECT COUNT(1) AS TotalVenues FROM dbo.Venues;
                SELECT 
                    COUNT(e.Id) AS TotalEvents,
                    SUM(CASE WHEN e.EndDateTime > GETUTCDATE() THEN 1 ELSE 0 END) AS UpcomingEvents,
                    SUM(CASE WHEN e.EndDateTime <= GETUTCDATE() THEN 1 ELSE 0 END) AS CompletedEvents
                FROM dbo.Events e;

                SELECT COUNT(1) AS TotalRegistrations FROM dbo.EventRegistrations WHERE Status = 'Confirmed';

                -- 8. Notifications
                SELECT 
                    COUNT(n.Id) AS TotalNotifications,
                    SUM(CASE WHEN n.IsRead = 1 THEN 1 ELSE 0 END) AS ReadNotifications,
                    SUM(CASE WHEN n.IsRead = 0 THEN 1 ELSE 0 END) AS UnreadNotifications
                FROM dbo.Notifications n;
            ";

            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            // 1. Students
            if (await reader.ReadAsync())
            {
                kpi.TotalRegisteredStudents = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                kpi.ActiveStudents = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                kpi.DeactivatedStudents = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                kpi.UnverifiedStudents = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            }

            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.MasterListIntakeTotal = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            }

            // 2. Hostels
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalHostels = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                kpi.TotalBedsCapacity = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            }

            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.OccupiedBeds = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                kpi.AvailableBeds = Math.Max(0, kpi.TotalBedsCapacity - kpi.OccupiedBeds);
                kpi.BedOccupancyPercentage = kpi.TotalBedsCapacity > 0 
                    ? Math.Round(((double)kpi.OccupiedBeds / kpi.TotalBedsCapacity) * 100, 1) 
                    : 0;
            }

            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.PendingHostelApplications = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                kpi.UnallocatedStudentsCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            }

            // 3. Labs
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalLabs = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            }
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalLabSeats = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            }
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.ConfirmedLabBookings = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                kpi.ActiveLabHolds = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                kpi.ExpiredOrCancelledBookings = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                kpi.LabUtilizationPercentage = kpi.TotalLabSeats > 0 
                    ? Math.Round(((double)kpi.ConfirmedLabBookings / Math.Max(1, kpi.TotalLabSeats * 10)) * 100, 1) 
                    : 0;
            }

            // 4. Billing
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalBilledAmount = reader.IsDBNull(0) ? 0m : reader.GetDecimal(0);
                kpi.TotalCollectedAmount = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                kpi.TotalOutstandingAmount = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
                kpi.TotalPaidInvoices = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                kpi.TotalUnpaidInvoices = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                kpi.CollectionRatePercentage = kpi.TotalBilledAmount > 0 
                    ? Math.Round(((double)kpi.TotalCollectedAmount / (double)kpi.TotalBilledAmount) * 100, 1) 
                    : 0;
            }

            // 5. Complaints
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalComplaints = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                kpi.PendingComplaints = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                kpi.InProgressComplaints = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                kpi.ResolvedComplaints = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                kpi.ComplaintResolutionRatePercentage = kpi.TotalComplaints > 0 
                    ? Math.Round(((double)kpi.ResolvedComplaints / kpi.TotalComplaints) * 100, 1) 
                    : 0;
            }

            // 6. Certificates
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalCertificateRequests = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                kpi.PendingCertificateRequests = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                kpi.ApprovedCertificateRequests = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                kpi.RejectedCertificateRequests = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            }

            // 7. Events
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalVenues = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            }
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalEvents = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                kpi.UpcomingEvents = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                kpi.CompletedEvents = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            }
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalEventRegistrations = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            }

            // 8. Notifications
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                kpi.TotalNotificationsSent = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                kpi.ReadNotificationsCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                kpi.UnreadNotificationsCount = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            }

            return kpi;
        }

        // ==========================================
        // 2. STUDENT REPORT (Summary & Subreport)
        // ==========================================
        public async Task<PagedReportResultDto<StudentDetailItemDto>> GetStudentReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<StudentDetailItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            // 1. Fetch Group Summaries by Faculty
            var summaryQuery = @"
                SELECT 
                    f.Id AS FacultyId,
                    f.Name AS FacultyName,
                    COUNT(s.Id) AS TotalEnrolled,
                    SUM(CASE WHEN s.DeactivatedAt IS NULL AND s.EmailVerified = 1 THEN 1 ELSE 0 END) AS ActiveCount,
                    SUM(CASE WHEN s.DeactivatedAt IS NOT NULL THEN 1 ELSE 0 END) AS DeactivatedCount,
                    SUM(CASE WHEN s.EmailVerified = 0 THEN 1 ELSE 0 END) AS UnverifiedCount
                FROM dbo.Faculties f
                LEFT JOIN dbo.Students s ON f.Id = s.FacultyId
                GROUP BY f.Id, f.Name
                ORDER BY f.Name ASC;
            ";

            var summaryList = new List<StudentSummaryItemDto>();
            int grandTotal = 0, grandActive = 0, grandDeactivated = 0, grandUnverified = 0;

            await using (var cmdSummary = new SqlCommand(summaryQuery, conn))
            await using (var reader = await cmdSummary.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var item = new StudentSummaryItemDto
                    {
                        FacultyId = reader.GetInt32(0),
                        FacultyName = reader.GetString(1),
                        TotalEnrolled = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                        ActiveCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        DeactivatedCount = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        UnverifiedCount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
                    };
                    grandTotal += item.TotalEnrolled;
                    grandActive += item.ActiveCount;
                    grandDeactivated += item.DeactivatedCount;
                    grandUnverified += item.UnverifiedCount;
                    summaryList.Add(item);
                }
            }

            foreach (var sum in summaryList)
            {
                sum.PercentageOfTotal = grandTotal > 0 ? Math.Round(((double)sum.TotalEnrolled / grandTotal) * 100, 1) : 0;
            }

            result.SummaryData = new StudentReportDto
            {
                FacultySummaries = summaryList,
                GrandTotalStudents = grandTotal,
                GrandTotalActive = grandActive,
                GrandTotalDeactivated = grandDeactivated,
                GrandTotalUnverified = grandUnverified
            };

            // 2. Build Paginated Detail / Subreport Query
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (filter.FacultyIds != null && filter.FacultyIds.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.FacultyIds.Count; i++)
                {
                    var p = $"@fac_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.FacultyIds[i]));
                }
                whereClauses.Add($"s.FacultyId IN ({string.Join(",", pNames)})");
            }

            if (filter.DrilldownId.HasValue && filter.DrilldownId.Value > 0)
            {
                whereClauses.Add("s.FacultyId = @drilldownFacId");
                parameters.Add(new SqlParameter("@drilldownFacId", filter.DrilldownId.Value));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR u.Email LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var statusConditions = new List<string>();
                foreach (var st in filter.Statuses)
                {
                    if (st.Equals("Active", StringComparison.OrdinalIgnoreCase))
                        statusConditions.Add("(s.DeactivatedAt IS NULL AND s.EmailVerified = 1)");
                    else if (st.Equals("Deactivated", StringComparison.OrdinalIgnoreCase))
                        statusConditions.Add("s.DeactivatedAt IS NOT NULL");
                    else if (st.Equals("Unverified", StringComparison.OrdinalIgnoreCase))
                        statusConditions.Add("s.EmailVerified = 0");
                }
                if (statusConditions.Count > 0)
                {
                    whereClauses.Add($"({string.Join(" OR ", statusConditions)})");
                }
            }

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Students s
                INNER JOIN dbo.Users u ON s.UserId = u.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                {whereSql};
            ";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = (int)(await cmdCount.ExecuteScalarAsync() ?? 0);
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    s.Id AS StudentId,
                    s.IndexNumber,
                    s.FullName,
                    u.Email,
                    s.FacultyId,
                    f.Name AS FacultyName,
                    s.ContactDetails,
                    s.EmailVerified,
                    s.DeactivatedAt,
                    CASE 
                        WHEN s.DeactivatedAt IS NOT NULL THEN 'Deactivated'
                        WHEN s.EmailVerified = 0 THEN 'Unverified'
                        ELSE 'Active'
                    END AS Status,
                    (SELECT TOP 1 City FROM dbo.StudentAddresses WHERE StudentId = s.Id) AS City,
                    (SELECT TOP 1 PhoneNumber FROM dbo.StudentPhoneNumbers WHERE StudentId = s.Id) AS PhoneNumber
                FROM dbo.Students s
                INNER JOIN dbo.Users u ON s.UserId = u.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                {whereSql}
                ORDER BY s.Id DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new StudentDetailItemDto
                    {
                        StudentId = reader.GetInt32(0),
                        IndexNumber = reader.GetString(1),
                        FullName = reader.GetString(2),
                        Email = reader.GetString(3),
                        FacultyId = reader.GetInt32(4),
                        FacultyName = reader.GetString(5),
                        EmailVerified = reader.GetBoolean(7),
                        DeactivatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                        Status = reader.GetString(9),
                        City = reader.IsDBNull(10) ? null : reader.GetString(10),
                        ContactPhone = reader.IsDBNull(11) ? null : reader.GetString(11)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 3. HOSTEL REPORT (Summary & Subreport)
        // ==========================================
        public async Task<PagedReportResultDto<HostelDetailItemDto>> GetHostelReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<HostelDetailItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            // 1. Summary by Hostel
            var summarySql = @"
                SELECT 
                    h.Id AS HostelId,
                    h.Name AS HostelName,
                    COUNT(DISTINCT r.Id) AS TotalRooms,
                    ISNULL(SUM(r.MaxCapacity), 0) AS TotalCapacity,
                    (SELECT COUNT(1) FROM dbo.HostelApplications ha WHERE ha.PreferredHostelId = h.Id AND ha.Status = 'RoomAssigned' AND ha.AssignedRoomId IS NOT NULL) AS OccupiedBeds,
                    (SELECT COUNT(1) FROM dbo.HostelApplications ha WHERE ha.PreferredHostelId = h.Id AND ha.Status = 'Pending') AS PendingApplications
                FROM dbo.Hostels h
                LEFT JOIN dbo.Rooms r ON h.Id = r.HostelId AND r.IsActive = 1
                WHERE h.IsActive = 1
                GROUP BY h.Id, h.Name
                ORDER BY h.Name ASC;
            ";

            var summaries = new List<HostelSummaryItemDto>();
            int grandBeds = 0, grandOccupied = 0;

            await using (var cmdSum = new SqlCommand(summarySql, conn))
            await using (var reader = await cmdSum.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var cap = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    var occ = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                    var item = new HostelSummaryItemDto
                    {
                        HostelId = reader.GetInt32(0),
                        HostelName = reader.GetString(1),
                        TotalRooms = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                        TotalBedCapacity = cap,
                        OccupiedBeds = occ,
                        AvailableBeds = Math.Max(0, cap - occ),
                        OccupancyRate = cap > 0 ? Math.Round(((double)occ / cap) * 100, 1) : 0,
                        PendingApplicationsCount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5)
                    };
                    grandBeds += cap;
                    grandOccupied += occ;
                    summaries.Add(item);
                }
            }

            result.SummaryData = new HostelReportDto
            {
                HostelSummaries = summaries,
                GrandTotalBeds = grandBeds,
                GrandTotalOccupied = grandOccupied,
                GrandTotalAvailable = Math.Max(0, grandBeds - grandOccupied),
                OverallOccupancyPercentage = grandBeds > 0 ? Math.Round(((double)grandOccupied / grandBeds) * 100, 1) : 0
            };

            // 2. Detail Applications / Allocations Query
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (filter.HostelIds != null && filter.HostelIds.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.HostelIds.Count; i++)
                {
                    var p = $"@h_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.HostelIds[i]));
                }
                whereClauses.Add($"ha.PreferredHostelId IN ({string.Join(",", pNames)})");
            }

            if (filter.DrilldownId.HasValue && filter.DrilldownId.Value > 0)
            {
                whereClauses.Add("ha.PreferredHostelId = @drilldownHostelId");
                parameters.Add(new SqlParameter("@drilldownHostelId", filter.DrilldownId.Value));
            }

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.Statuses.Count; i++)
                {
                    var p = $"@st_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.Statuses[i]));
                }
                whereClauses.Add($"ha.Status IN ({string.Join(",", pNames)})");
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR h.Name LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            if (filter.DateFrom.HasValue)
            {
                whereClauses.Add("ha.CreatedAt >= @dateFrom");
                parameters.Add(new SqlParameter("@dateFrom", filter.DateFrom.Value));
            }

            if (filter.DateTo.HasValue)
            {
                whereClauses.Add("ha.CreatedAt <= @dateTo");
                parameters.Add(new SqlParameter("@dateTo", filter.DateTo.Value));
            }

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.HostelApplications ha
                INNER JOIN dbo.Students s ON ha.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.Hostels h ON ha.PreferredHostelId = h.Id
                {whereSql};
            ";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = (int)(await cmdCount.ExecuteScalarAsync() ?? 0);
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    ha.Id AS ApplicationId,
                    ha.StudentId,
                    s.FullName AS StudentName,
                    s.IndexNumber,
                    f.Name AS FacultyName,
                    h.Name AS PreferredHostelName,
                    ha.TermSemester,
                    ha.Status,
                    r.RoomNumber AS AssignedRoomNumber,
                    ha.CreatedAt AS ApplicationDate
                FROM dbo.HostelApplications ha
                INNER JOIN dbo.Students s ON ha.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.Hostels h ON ha.PreferredHostelId = h.Id
                LEFT JOIN dbo.Rooms r ON ha.AssignedRoomId = r.Id
                {whereSql}
                ORDER BY ha.Id DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new HostelDetailItemDto
                    {
                        ApplicationId = reader.GetInt32(0),
                        StudentId = reader.GetInt32(1),
                        StudentName = reader.GetString(2),
                        IndexNumber = reader.GetString(3),
                        FacultyName = reader.GetString(4),
                        PreferredHostelName = reader.GetString(5),
                        TermSemester = reader.GetString(6),
                        Status = reader.GetString(7),
                        AssignedRoomNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                        ApplicationDate = reader.GetDateTime(9)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 4. LAB REPORT (Summary & Subreport)
        // ==========================================
        public async Task<PagedReportResultDto<LabBookingDetailItemDto>> GetLabReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<LabBookingDetailItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            // 1. Lab Summaries
            var summarySql = @"
                SELECT 
                    l.Id AS LabId,
                    l.Name AS LabName,
                    l.Capacity,
                    (SELECT COUNT(1) FROM dbo.LabSeats ls WHERE ls.LabId = l.Id AND ls.IsBroken = 0) AS ConfiguredSeats,
                    (SELECT COUNT(1) FROM dbo.LabBookings lb WHERE lb.LabId = l.Id AND lb.Status = 'Confirmed') AS ConfirmedBookings,
                    (SELECT COUNT(1) FROM dbo.LabBookings lb WHERE lb.LabId = l.Id AND lb.Status = 'Held' AND lb.ExpiresAt > GETUTCDATE()) AS ActiveHolds,
                    (SELECT COUNT(1) FROM dbo.LabBookings lb WHERE lb.LabId = l.Id AND (lb.Status IN ('Cancelled', 'Expired') OR (lb.Status = 'Held' AND lb.ExpiresAt <= GETUTCDATE()))) AS CancelledOrExpired
                FROM dbo.Labs l
                WHERE l.IsActive = 1
                ORDER BY l.Name ASC;
            ";

            var summaries = new List<LabSummaryItemDto>();
            int grandCapacity = 0, grandBookings = 0, grandHolds = 0;

            await using (var cmdSum = new SqlCommand(summarySql, conn))
            await using (var reader = await cmdSum.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var cap = reader.GetInt32(2);
                    var conf = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                    var holds = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                    var item = new LabSummaryItemDto
                    {
                        LabId = reader.GetInt32(0),
                        LabName = reader.GetString(1),
                        TotalCapacity = cap,
                        TotalConfiguredSeats = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                        ConfirmedBookings = conf,
                        ActiveHolds = holds,
                        CancelledOrExpired = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                        UtilizationRate = cap > 0 ? Math.Round(((double)conf / Math.Max(1, cap * 10)) * 100, 1) : 0
                    };
                    grandCapacity += cap;
                    grandBookings += conf;
                    grandHolds += holds;
                    summaries.Add(item);
                }
            }

            result.SummaryData = new LabReportDto
            {
                LabSummaries = summaries,
                GrandTotalLabs = summaries.Count,
                GrandTotalCapacity = grandCapacity,
                GrandTotalBookings = grandBookings,
                GrandTotalActiveHolds = grandHolds,
                OverallUtilizationPercentage = grandCapacity > 0 ? Math.Round(((double)grandBookings / Math.Max(1, grandCapacity * 10)) * 100, 1) : 0
            };

            // 2. Booking Details / Subreport Query
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (filter.LabIds != null && filter.LabIds.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.LabIds.Count; i++)
                {
                    var p = $"@l_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.LabIds[i]));
                }
                whereClauses.Add($"lb.LabId IN ({string.Join(",", pNames)})");
            }

            if (filter.DrilldownId.HasValue && filter.DrilldownId.Value > 0)
            {
                whereClauses.Add("lb.LabId = @drilldownLabId");
                parameters.Add(new SqlParameter("@drilldownLabId", filter.DrilldownId.Value));
            }

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.Statuses.Count; i++)
                {
                    var p = $"@st_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.Statuses[i]));
                }
                whereClauses.Add($"lb.Status IN ({string.Join(",", pNames)})");
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR l.Name LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            if (filter.DateFrom.HasValue)
            {
                whereClauses.Add("lb.BookingDate >= @dateFrom");
                parameters.Add(new SqlParameter("@dateFrom", filter.DateFrom.Value));
            }

            if (filter.DateTo.HasValue)
            {
                whereClauses.Add("lb.BookingDate <= @dateTo");
                parameters.Add(new SqlParameter("@dateTo", filter.DateTo.Value));
            }

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.LabBookings lb
                INNER JOIN dbo.Students s ON lb.StudentId = s.Id
                INNER JOIN dbo.Labs l ON lb.LabId = l.Id
                {whereSql};
            ";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = (int)(await cmdCount.ExecuteScalarAsync() ?? 0);
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    lb.Id AS BookingId,
                    lb.LabId,
                    l.Name AS LabName,
                    lb.StudentId,
                    s.FullName AS StudentName,
                    s.IndexNumber,
                    ls.SeatNumber,
                    lb.BookingDate,
                    lb.TimeSlot,
                    lb.Status,
                    lb.ExpiresAt
                FROM dbo.LabBookings lb
                INNER JOIN dbo.Students s ON lb.StudentId = s.Id
                INNER JOIN dbo.Labs l ON lb.LabId = l.Id
                LEFT JOIN dbo.LabSeats ls ON lb.SeatId = ls.Id
                {whereSql}
                ORDER BY lb.BookingDate DESC, lb.Id DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new LabBookingDetailItemDto
                    {
                        BookingId = reader.GetInt32(0),
                        LabId = reader.GetInt32(1),
                        LabName = reader.GetString(2),
                        StudentId = reader.GetInt32(3),
                        StudentName = reader.GetString(4),
                        IndexNumber = reader.GetString(5),
                        SeatNumber = reader.IsDBNull(6) ? null : reader.GetString(6),
                        BookingDate = reader.GetDateTime(7),
                        TimeSlot = reader.GetString(8),
                        Status = reader.GetString(9),
                        ExpiresAt = reader.GetDateTime(10)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 5. BILLING & FEES REPORT (Summary & Subreport)
        // ==========================================
        public async Task<PagedReportResultDto<PaymentDetailItemDto>> GetBillingReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<PaymentDetailItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            // 1. Fee Types Summaries with Subtotals
            var summarySql = @"
                SELECT 
                    ft.Id AS FeeTypeId,
                    ft.Name AS FeeTypeName,
                    COUNT(fp.Id) AS TotalInvoices,
                    ISNULL(SUM(fp.Amount), 0) AS TotalBilled,
                    ISNULL(SUM(CASE WHEN fp.Status = 'Paid' THEN fp.Amount ELSE 0 END), 0) AS TotalPaid,
                    ISNULL(SUM(CASE WHEN fp.Status = 'Outstanding' THEN fp.Amount ELSE 0 END), 0) AS TotalOutstanding
                FROM dbo.FeeTypes ft
                LEFT JOIN dbo.FeePayments fp ON ft.Id = fp.FeeTypeId
                WHERE ft.IsActive = 1
                GROUP BY ft.Id, ft.Name
                ORDER BY ft.Name ASC;
            ";

            var summaries = new List<FeeTypeSummaryItemDto>();
            decimal grandBilled = 0m, grandPaid = 0m, grandOutstanding = 0m;
            int grandInvoices = 0;

            await using (var cmdSum = new SqlCommand(summarySql, conn))
            await using (var reader = await cmdSum.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var billed = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3);
                    var paid = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4);
                    var outst = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5);
                    var inv = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

                    var item = new FeeTypeSummaryItemDto
                    {
                        FeeTypeId = reader.GetInt32(0),
                        FeeTypeName = reader.GetString(1),
                        TotalInvoices = inv,
                        TotalBilled = billed,
                        TotalPaid = paid,
                        TotalOutstanding = outst,
                        CollectionRate = billed > 0 ? Math.Round(((double)paid / (double)billed) * 100, 1) : 0
                    };
                    grandBilled += billed;
                    grandPaid += paid;
                    grandOutstanding += outst;
                    grandInvoices += inv;
                    summaries.Add(item);
                }
            }

            result.SummaryData = new BillingReportDto
            {
                FeeTypeSummaries = summaries,
                GrandTotalBilled = grandBilled,
                GrandTotalPaid = grandPaid,
                GrandTotalOutstanding = grandOutstanding,
                GrandTotalInvoices = grandInvoices,
                OverallCollectionPercentage = grandBilled > 0 ? Math.Round(((double)grandPaid / (double)grandBilled) * 100, 1) : 0
            };

            // 2. Paginated Ledger Details Query
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (filter.FeeTypeIds != null && filter.FeeTypeIds.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.FeeTypeIds.Count; i++)
                {
                    var p = $"@ft_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.FeeTypeIds[i]));
                }
                whereClauses.Add($"fp.FeeTypeId IN ({string.Join(",", pNames)})");
            }

            if (filter.DrilldownId.HasValue && filter.DrilldownId.Value > 0)
            {
                whereClauses.Add("fp.FeeTypeId = @drilldownFeeTypeId");
                parameters.Add(new SqlParameter("@drilldownFeeTypeId", filter.DrilldownId.Value));
            }

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.Statuses.Count; i++)
                {
                    var p = $"@st_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.Statuses[i]));
                }
                whereClauses.Add($"fp.Status IN ({string.Join(",", pNames)})");
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR ft.Name LIKE @search OR fp.BillingPeriod LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            if (filter.DateFrom.HasValue)
            {
                whereClauses.Add("fp.PaidAt >= @dateFrom");
                parameters.Add(new SqlParameter("@dateFrom", filter.DateFrom.Value));
            }

            if (filter.DateTo.HasValue)
            {
                whereClauses.Add("fp.PaidAt <= @dateTo");
                parameters.Add(new SqlParameter("@dateTo", filter.DateTo.Value));
            }

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.FeePayments fp
                INNER JOIN dbo.Students s ON fp.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.FeeTypes ft ON fp.FeeTypeId = ft.Id
                {whereSql};
            ";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = (int)(await cmdCount.ExecuteScalarAsync() ?? 0);
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    fp.Id AS InvoiceId,
                    fp.StudentId,
                    s.FullName AS StudentName,
                    s.IndexNumber,
                    f.Name AS FacultyName,
                    fp.FeeTypeId,
                    ft.Name AS FeeTypeName,
                    fp.Amount,
                    fp.BillingPeriod,
                    fp.Description,
                    fp.Status,
                    fp.PaidAt
                FROM dbo.FeePayments fp
                INNER JOIN dbo.Students s ON fp.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.FeeTypes ft ON fp.FeeTypeId = ft.Id
                {whereSql}
                ORDER BY fp.Id DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new PaymentDetailItemDto
                    {
                        InvoiceId = reader.GetInt32(0),
                        StudentId = reader.GetInt32(1),
                        StudentName = reader.GetString(2),
                        IndexNumber = reader.GetString(3),
                        FacultyName = reader.GetString(4),
                        FeeTypeId = reader.GetInt32(5),
                        FeeTypeName = reader.GetString(6),
                        Amount = reader.GetDecimal(7),
                        BillingPeriod = reader.GetString(8),
                        Description = reader.IsDBNull(9) ? null : reader.GetString(9),
                        Status = reader.GetString(10),
                        PaidAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 6. COMPLAINT REPORT (Summary & Subreport)
        // ==========================================
        public async Task<PagedReportResultDto<ComplaintDetailItemDto>> GetComplaintReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<ComplaintDetailItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            // 1. Category Summaries
            var summarySql = @"
                SELECT 
                    cc.Id AS CategoryId,
                    cc.Name AS CategoryName,
                    COUNT(c.Id) AS TotalFiled,
                    SUM(CASE WHEN c.Status = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
                    SUM(CASE WHEN c.Status = 'In Progress' THEN 1 ELSE 0 END) AS InProgressCount,
                    SUM(CASE WHEN c.Status = 'Resolved' THEN 1 ELSE 0 END) AS ResolvedCount
                FROM dbo.ComplaintCategories cc
                LEFT JOIN dbo.Complaints c ON cc.Id = c.CategoryId
                WHERE cc.IsActive = 1
                GROUP BY cc.Id, cc.Name
                ORDER BY cc.Name ASC;
            ";

            var summaries = new List<ComplaintCategorySummaryItemDto>();
            int grandTotal = 0, grandPending = 0, grandProgress = 0, grandResolved = 0;

            await using (var cmdSum = new SqlCommand(summarySql, conn))
            await using (var reader = await cmdSum.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var tot = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    var pend = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    var prog = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                    var res = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);

                    var item = new ComplaintCategorySummaryItemDto
                    {
                        CategoryId = reader.GetInt32(0),
                        CategoryName = reader.GetString(1),
                        TotalFiled = tot,
                        PendingCount = pend,
                        InProgressCount = prog,
                        ResolvedCount = res,
                        ResolutionRate = tot > 0 ? Math.Round(((double)res / tot) * 100, 1) : 0
                    };
                    grandTotal += tot;
                    grandPending += pend;
                    grandProgress += prog;
                    grandResolved += res;
                    summaries.Add(item);
                }
            }

            result.SummaryData = new ComplaintReportDto
            {
                CategorySummaries = summaries,
                GrandTotalComplaints = grandTotal,
                GrandTotalPending = grandPending,
                GrandTotalInProgress = grandProgress,
                GrandTotalResolved = grandResolved,
                OverallResolutionPercentage = grandTotal > 0 ? Math.Round(((double)grandResolved / grandTotal) * 100, 1) : 0
            };

            // 2. Complaint Details Query
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (filter.CategoryIds != null && filter.CategoryIds.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.CategoryIds.Count; i++)
                {
                    var p = $"@cat_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.CategoryIds[i]));
                }
                whereClauses.Add($"c.CategoryId IN ({string.Join(",", pNames)})");
            }

            if (filter.DrilldownId.HasValue && filter.DrilldownId.Value > 0)
            {
                whereClauses.Add("c.CategoryId = @drilldownCatId");
                parameters.Add(new SqlParameter("@drilldownCatId", filter.DrilldownId.Value));
            }

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.Statuses.Count; i++)
                {
                    var p = $"@st_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.Statuses[i]));
                }
                whereClauses.Add($"c.Status IN ({string.Join(",", pNames)})");
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR c.Description LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            if (filter.DateFrom.HasValue)
            {
                whereClauses.Add("c.CreatedAt >= @dateFrom");
                parameters.Add(new SqlParameter("@dateFrom", filter.DateFrom.Value));
            }

            if (filter.DateTo.HasValue)
            {
                whereClauses.Add("c.CreatedAt <= @dateTo");
                parameters.Add(new SqlParameter("@dateTo", filter.DateTo.Value));
            }

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Complaints c
                INNER JOIN dbo.Students s ON c.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.ComplaintCategories cc ON c.CategoryId = cc.Id
                {whereSql};
            ";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = (int)(await cmdCount.ExecuteScalarAsync() ?? 0);
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    c.Id AS ComplaintId,
                    c.StudentId,
                    s.FullName AS StudentName,
                    s.IndexNumber,
                    f.Name AS FacultyName,
                    c.CategoryId,
                    cc.Name AS CategoryName,
                    c.Description,
                    c.Status,
                    c.ResolutionNote,
                    c.CreatedAt
                FROM dbo.Complaints c
                INNER JOIN dbo.Students s ON c.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.ComplaintCategories cc ON c.CategoryId = cc.Id
                {whereSql}
                ORDER BY c.Id DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new ComplaintDetailItemDto
                    {
                        ComplaintId = reader.GetInt32(0),
                        StudentId = reader.GetInt32(1),
                        StudentName = reader.GetString(2),
                        IndexNumber = reader.GetString(3),
                        FacultyName = reader.GetString(4),
                        CategoryId = reader.GetInt32(5),
                        CategoryName = reader.GetString(6),
                        Description = reader.GetString(7),
                        Status = reader.GetString(8),
                        ResolutionNote = reader.IsDBNull(9) ? null : reader.GetString(9),
                        CreatedAt = reader.GetDateTime(10)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 7. CERTIFICATE REPORT (Summary & Subreport)
        // ==========================================
        public async Task<PagedReportResultDto<CertificateRequestDetailItemDto>> GetCertificateReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<CertificateRequestDetailItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            // 1. Certificate Type Summaries
            var summarySql = @"
                SELECT 
                    ct.Id AS CertificateTypeId,
                    ct.Name AS CertificateTypeName,
                    COUNT(cr.Id) AS TotalRequested,
                    SUM(CASE WHEN cr.Status = 'Pending' THEN 1 ELSE 0 END) AS PendingCount,
                    SUM(CASE WHEN cr.Status = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
                    SUM(CASE WHEN cr.Status = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount
                FROM dbo.CertificateTypes ct
                LEFT JOIN dbo.CertificateRequests cr ON ct.Id = cr.CertificateTypeId
                WHERE ct.IsActive = 1
                GROUP BY ct.Id, ct.Name
                ORDER BY ct.Name ASC;
            ";

            var summaries = new List<CertificateTypeSummaryItemDto>();
            int grandTotal = 0, grandPending = 0, grandApproved = 0, grandRejected = 0;

            await using (var cmdSum = new SqlCommand(summarySql, conn))
            await using (var reader = await cmdSum.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var tot = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    var pend = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    var app = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                    var rej = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);

                    var item = new CertificateTypeSummaryItemDto
                    {
                        CertificateTypeId = reader.GetInt32(0),
                        CertificateTypeName = reader.GetString(1),
                        TotalRequested = tot,
                        PendingCount = pend,
                        ApprovedCount = app,
                        RejectedCount = rej,
                        ApprovalRate = tot > 0 ? Math.Round(((double)app / tot) * 100, 1) : 0
                    };
                    grandTotal += tot;
                    grandPending += pend;
                    grandApproved += app;
                    grandRejected += rej;
                    summaries.Add(item);
                }
            }

            result.SummaryData = new CertificateReportDto
            {
                TypeSummaries = summaries,
                GrandTotalRequests = grandTotal,
                GrandTotalPending = grandPending,
                GrandTotalApproved = grandApproved,
                GrandTotalRejected = grandRejected,
                OverallApprovalPercentage = grandTotal > 0 ? Math.Round(((double)grandApproved / grandTotal) * 100, 1) : 0
            };

            // 2. Certificate Detail Subreport Query
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (filter.CertificateTypeIds != null && filter.CertificateTypeIds.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.CertificateTypeIds.Count; i++)
                {
                    var p = $"@ct_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.CertificateTypeIds[i]));
                }
                whereClauses.Add($"cr.CertificateTypeId IN ({string.Join(",", pNames)})");
            }

            if (filter.DrilldownId.HasValue && filter.DrilldownId.Value > 0)
            {
                whereClauses.Add("cr.CertificateTypeId = @drilldownCertTypeId");
                parameters.Add(new SqlParameter("@drilldownCertTypeId", filter.DrilldownId.Value));
            }

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.Statuses.Count; i++)
                {
                    var p = $"@st_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.Statuses[i]));
                }
                whereClauses.Add($"cr.Status IN ({string.Join(",", pNames)})");
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR ct.Name LIKE @search OR cr.Reason LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            if (filter.DateFrom.HasValue)
            {
                whereClauses.Add("cr.RequestedAt >= @dateFrom");
                parameters.Add(new SqlParameter("@dateFrom", filter.DateFrom.Value));
            }

            if (filter.DateTo.HasValue)
            {
                whereClauses.Add("cr.RequestedAt <= @dateTo");
                parameters.Add(new SqlParameter("@dateTo", filter.DateTo.Value));
            }

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.CertificateRequests cr
                INNER JOIN dbo.Students s ON cr.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.CertificateTypes ct ON cr.CertificateTypeId = ct.Id
                {whereSql};
            ";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = (int)(await cmdCount.ExecuteScalarAsync() ?? 0);
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    cr.Id AS RequestId,
                    cr.StudentId,
                    s.FullName AS StudentName,
                    s.IndexNumber,
                    f.Name AS FacultyName,
                    cr.CertificateTypeId,
                    ct.Name AS CertificateTypeName,
                    cr.Reason,
                    cr.Status,
                    cr.RequestedAt
                FROM dbo.CertificateRequests cr
                INNER JOIN dbo.Students s ON cr.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.CertificateTypes ct ON cr.CertificateTypeId = ct.Id
                {whereSql}
                ORDER BY cr.Id DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new CertificateRequestDetailItemDto
                    {
                        RequestId = reader.GetInt32(0),
                        StudentId = reader.GetInt32(1),
                        StudentName = reader.GetString(2),
                        IndexNumber = reader.GetString(3),
                        FacultyName = reader.GetString(4),
                        CertificateTypeId = reader.GetInt32(5),
                        CertificateTypeName = reader.GetString(6),
                        Reason = reader.GetString(7),
                        Status = reader.GetString(8),
                        RequestedAt = reader.GetDateTime(9)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 8. EVENT REPORT (Summary & Subreport)
        // ==========================================
        public async Task<PagedReportResultDto<EventRegistrationDetailItemDto>> GetEventReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<EventRegistrationDetailItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            // 1. Event Summaries
            var summarySql = @"
                SELECT 
                    e.Id AS EventId,
                    e.Title,
                    v.Name AS VenueName,
                    v.Capacity AS VenueCapacity,
                    e.Capacity AS EventCapacity,
                    e.StartDateTime,
                    e.EndDateTime,
                    COUNT(er.Id) AS TotalRegistrations,
                    SUM(CASE WHEN er.Status = 'Confirmed' THEN 1 ELSE 0 END) AS ConfirmedRegistrations,
                    SUM(CASE WHEN er.Status = 'Held' AND er.ExpiresAt > GETUTCDATE() THEN 1 ELSE 0 END) AS ActiveHolds,
                    CASE WHEN e.EndDateTime <= GETUTCDATE() THEN 1 ELSE 0 END AS IsCompleted
                FROM dbo.Events e
                INNER JOIN dbo.Venues v ON e.VenueId = v.Id
                LEFT JOIN dbo.EventRegistrations er ON e.Id = er.EventId
                GROUP BY e.Id, e.Title, v.Name, v.Capacity, e.Capacity, e.StartDateTime, e.EndDateTime
                ORDER BY e.StartDateTime DESC;
            ";

            var summaries = new List<EventSummaryItemDto>();
            int grandEvents = 0, grandUpcoming = 0, grandCompleted = 0, grandRegs = 0;

            await using (var cmdSum = new SqlCommand(summarySql, conn))
            await using (var reader = await cmdSum.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var cap = reader.GetInt32(4);
                    var conf = reader.IsDBNull(8) ? 0 : reader.GetInt32(8);
                    var isComp = reader.GetInt32(10) == 1;

                    var item = new EventSummaryItemDto
                    {
                        EventId = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        VenueName = reader.GetString(2),
                        VenueCapacity = reader.GetInt32(3),
                        EventCapacity = cap,
                        StartDateTime = reader.GetDateTime(5),
                        EndDateTime = reader.GetDateTime(6),
                        TotalRegistrations = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                        ConfirmedRegistrations = conf,
                        ActiveHolds = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                        FillRate = cap > 0 ? Math.Round(((double)conf / cap) * 100, 1) : 0,
                        IsCompleted = isComp
                    };
                    grandEvents++;
                    if (isComp) grandCompleted++; else grandUpcoming++;
                    grandRegs += item.TotalRegistrations;
                    summaries.Add(item);
                }
            }

            result.SummaryData = new EventReportDto
            {
                EventSummaries = summaries,
                GrandTotalEvents = grandEvents,
                GrandTotalUpcoming = grandUpcoming,
                GrandTotalCompleted = grandCompleted,
                GrandTotalRegistrations = grandRegs
            };

            // 2. Attendee Registration Details Query
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (filter.EventIds != null && filter.EventIds.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.EventIds.Count; i++)
                {
                    var p = $"@ev_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.EventIds[i]));
                }
                whereClauses.Add($"er.EventId IN ({string.Join(",", pNames)})");
            }

            if (filter.DrilldownId.HasValue && filter.DrilldownId.Value > 0)
            {
                whereClauses.Add("er.EventId = @drilldownEventId");
                parameters.Add(new SqlParameter("@drilldownEventId", filter.DrilldownId.Value));
            }

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var pNames = new List<string>();
                for (int i = 0; i < filter.Statuses.Count; i++)
                {
                    var p = $"@st_{i}";
                    pNames.Add(p);
                    parameters.Add(new SqlParameter(p, filter.Statuses[i]));
                }
                whereClauses.Add($"er.Status IN ({string.Join(",", pNames)})");
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR e.Title LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.EventRegistrations er
                INNER JOIN dbo.Students s ON er.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.Events e ON er.EventId = e.Id
                {whereSql};
            ";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = (int)(await cmdCount.ExecuteScalarAsync() ?? 0);
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    er.Id AS RegistrationId,
                    er.EventId,
                    e.Title AS EventTitle,
                    er.StudentId,
                    s.FullName AS StudentName,
                    s.IndexNumber,
                    f.Name AS FacultyName,
                    er.Status,
                    er.ExpiresAt
                FROM dbo.EventRegistrations er
                INNER JOIN dbo.Students s ON er.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.Events e ON er.EventId = e.Id
                {whereSql}
                ORDER BY er.Id DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new EventRegistrationDetailItemDto
                    {
                        RegistrationId = reader.GetInt32(0),
                        EventId = reader.GetInt32(1),
                        EventTitle = reader.GetString(2),
                        StudentId = reader.GetInt32(3),
                        StudentName = reader.GetString(4),
                        IndexNumber = reader.GetString(5),
                        FacultyName = reader.GetString(6),
                        Status = reader.GetString(7),
                        ExpiresAt = reader.GetDateTime(8)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 9. NOTIFICATION REPORT (Summary & Subreport)
        // ==========================================
        public async Task<PagedReportResultDto<NotificationDetailItemDto>> GetNotificationReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<NotificationDetailItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 10 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            // 1. Notification Type Summaries
            var summarySql = @"
                SELECT 
                    n.Type,
                    COUNT(n.Id) AS TotalSent,
                    SUM(CASE WHEN n.IsRead = 1 THEN 1 ELSE 0 END) AS ReadCount,
                    SUM(CASE WHEN n.IsRead = 0 THEN 1 ELSE 0 END) AS UnreadCount
                FROM dbo.Notifications n
                GROUP BY n.Type
                ORDER BY TotalSent DESC;
            ";

            var summaries = new List<NotificationTypeSummaryItemDto>();
            int grandTotal = 0, grandRead = 0, grandUnread = 0;

            await using (var cmdSum = new SqlCommand(summarySql, conn))
            await using (var reader = await cmdSum.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var tot = reader.GetInt32(1);
                    var read = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    var unread = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                    var item = new NotificationTypeSummaryItemDto
                    {
                        Type = reader.GetString(0),
                        TotalSent = tot,
                        ReadCount = read,
                        UnreadCount = unread,
                        ReadRate = tot > 0 ? Math.Round(((double)read / tot) * 100, 1) : 0
                    };
                    grandTotal += tot;
                    grandRead += read;
                    grandUnread += unread;
                    summaries.Add(item);
                }
            }

            result.SummaryData = new NotificationReportDto
            {
                TypeSummaries = summaries,
                GrandTotalNotifications = grandTotal,
                GrandTotalRead = grandRead,
                GrandTotalUnread = grandUnread,
                OverallReadPercentage = grandTotal > 0 ? Math.Round(((double)grandRead / grandTotal) * 100, 1) : 0
            };

            // 2. Notification Dispatch Details Query
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(filter.DrilldownKey))
            {
                whereClauses.Add("n.Type = @drilldownType");
                parameters.Add(new SqlParameter("@drilldownType", filter.DrilldownKey.Trim()));
            }

            if (filter.Statuses != null && filter.Statuses.Count > 0)
            {
                var statusConditions = new List<string>();
                foreach (var st in filter.Statuses)
                {
                    if (st.Equals("Read", StringComparison.OrdinalIgnoreCase)) statusConditions.Add("n.IsRead = 1");
                    else if (st.Equals("Unread", StringComparison.OrdinalIgnoreCase)) statusConditions.Add("n.IsRead = 0");
                }
                if (statusConditions.Count > 0)
                {
                    whereClauses.Add($"({string.Join(" OR ", statusConditions)})");
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR n.Message LIKE @search OR n.Type LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            if (filter.DateFrom.HasValue)
            {
                whereClauses.Add("n.CreatedAt >= @dateFrom");
                parameters.Add(new SqlParameter("@dateFrom", filter.DateFrom.Value));
            }

            if (filter.DateTo.HasValue)
            {
                whereClauses.Add("n.CreatedAt <= @dateTo");
                parameters.Add(new SqlParameter("@dateTo", filter.DateTo.Value));
            }

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Notifications n
                INNER JOIN dbo.Students s ON n.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                {whereSql};
            ";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = (int)(await cmdCount.ExecuteScalarAsync() ?? 0);
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    n.Id AS NotificationId,
                    n.StudentId,
                    s.FullName AS StudentName,
                    s.IndexNumber,
                    f.Name AS FacultyName,
                    n.Type,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt
                FROM dbo.Notifications n
                INNER JOIN dbo.Students s ON n.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                {whereSql}
                ORDER BY n.Id DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new NotificationDetailItemDto
                    {
                        NotificationId = reader.GetInt32(0),
                        StudentId = reader.GetInt32(1),
                        StudentName = reader.GetString(2),
                        IndexNumber = reader.GetString(3),
                        FacultyName = reader.GetString(4),
                        Type = reader.GetString(5),
                        Message = reader.GetString(6),
                        IsRead = reader.GetBoolean(7),
                        CreatedAt = reader.GetDateTime(8)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 10. HOSTEL ROOMS & INVENTORY REPORT
        // ==========================================
        public async Task<PagedReportResultDto<HostelRoomDetailItemDto>> GetHostelRoomsReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<HostelRoomDetailItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 25 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var whereClauses = new List<string> { "1=1" };
            var parameters = new List<SqlParameter>();

            if (filter.HostelId.HasValue)
            {
                whereClauses.Add("r.HostelId = @hostelId");
                parameters.Add(new SqlParameter("@hostelId", filter.HostelId.Value));
            }
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (filter.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    whereClauses.Add("r.IsActive = 1");
                else if (filter.Status.Equals("Maintenance", StringComparison.OrdinalIgnoreCase))
                    whereClauses.Add("r.IsActive = 0");
            }
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(r.RoomNumber LIKE @search OR h.Name LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

            const string summarySql = @"
                SELECT 
                    COUNT(r.Id) AS TotalRooms,
                    ISNULL(SUM(r.MaxCapacity), 0) AS TotalBeds,
                    ISNULL(SUM(occ.OccupiedCount), 0) AS TotalOccupied,
                    SUM(CASE WHEN r.IsActive = 0 THEN 1 ELSE 0 END) AS MaintenanceCount
                FROM dbo.Rooms r
                INNER JOIN dbo.Hostels h ON r.HostelId = h.Id
                OUTER APPLY (
                    SELECT COUNT(1) AS OccupiedCount 
                    FROM dbo.HostelApplications ha 
                    WHERE ha.AssignedRoomId = r.Id AND ha.Status = 'RoomAssigned'
                ) occ;
            ";

            await using (var cmdSummary = new SqlCommand(summarySql, conn))
            {
                await using var reader = await cmdSummary.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int totalRooms = reader.GetInt32(0);
                    int totalBeds = reader.GetInt32(1);
                    int totalOccupied = reader.GetInt32(2);
                    int maintCount = reader.GetInt32(3);
                    int totalVacant = Math.Max(0, totalBeds - totalOccupied);
                    double rate = totalBeds > 0 ? Math.Round((double)totalOccupied / totalBeds * 100, 1) : 0.0;

                    result.GrandTotals = new HostelRoomReportDto
                    {
                        GrandTotalRooms = totalRooms,
                        GrandTotalBeds = totalBeds,
                        GrandTotalOccupied = totalOccupied,
                        GrandTotalVacant = totalVacant,
                        OverallOccupancyPercentage = rate,
                        MaintenanceRoomsCount = maintCount
                    };
                }
            }

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Rooms r 
                INNER JOIN dbo.Hostels h ON r.HostelId = h.Id 
                {whereSql};";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    r.Id AS RoomId,
                    r.RoomNumber,
                    r.HostelId,
                    h.Name AS HostelName,
                    ISNULL(TRY_CAST(LEFT(r.RoomNumber, 1) AS INT), 1) AS FloorNumber,
                    r.MaxCapacity AS Capacity,
                    ISNULL(occ.OccupiedCount, 0) AS OccupiedBeds,
                    CASE WHEN r.MaxCapacity >= 4 THEN 'Shared Hall' WHEN r.MaxCapacity = 3 THEN 'Triple Room' WHEN r.MaxCapacity = 2 THEN 'Double Room' ELSE 'Single Room' END AS RoomType,
                    CASE WHEN r.IsActive = 1 THEN 'Active' ELSE 'Maintenance' END AS Status,
                    CAST(150.00 AS DECIMAL(18,2)) AS FeePerSemester
                FROM dbo.Rooms r
                INNER JOIN dbo.Hostels h ON r.HostelId = h.Id
                OUTER APPLY (
                    SELECT COUNT(1) AS OccupiedCount 
                    FROM dbo.HostelApplications ha 
                    WHERE ha.AssignedRoomId = r.Id AND ha.Status = 'RoomAssigned'
                ) occ
                {whereSql}
                ORDER BY h.Name ASC, r.RoomNumber ASC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int cap = reader.GetInt32(5);
                    int occ = reader.GetInt32(6);
                    result.Items.Add(new HostelRoomDetailItemDto
                    {
                        RoomId = reader.GetInt32(0),
                        RoomNumber = reader.GetString(1),
                        HostelId = reader.GetInt32(2),
                        HostelName = reader.GetString(3),
                        FloorNumber = reader.GetInt32(4),
                        Capacity = cap,
                        OccupiedBeds = occ,
                        AvailableBeds = Math.Max(0, cap - occ),
                        RoomType = reader.GetString(7),
                        Status = reader.GetString(8),
                        FeePerSemester = reader.GetDecimal(9)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 11. PENDING HOSTEL APPLICATIONS REPORT
        // ==========================================
        public async Task<PagedReportResultDto<PendingHostelApplicationItemDto>> GetPendingHostelApplicationsReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<PendingHostelApplicationItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 25 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var whereClauses = new List<string> { "ha.Status IN ('Pending', 'Hold')" };
            var parameters = new List<SqlParameter>();

            if (filter.HostelId.HasValue)
            {
                whereClauses.Add("ha.PreferredHostelId = @hostelId");
                parameters.Add(new SqlParameter("@hostelId", filter.HostelId.Value));
            }
            if (filter.StartDate.HasValue)
            {
                whereClauses.Add("ha.CreatedAt >= @startDate");
                parameters.Add(new SqlParameter("@startDate", filter.StartDate.Value));
            }
            if (filter.EndDate.HasValue)
            {
                whereClauses.Add("ha.CreatedAt <= @endDate");
                parameters.Add(new SqlParameter("@endDate", filter.EndDate.Value));
            }
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR h.Name LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

            const string summarySql = @"
                SELECT 
                    COUNT(ha.Id) AS TotalPending,
                    ISNULL(SUM(CASE WHEN ha.Status = 'RoomAssigned' THEN 1 ELSE 0 END), 0) AS TotalAllocated,
                    ISNULL(SUM(CASE WHEN ha.Status = 'Rejected' THEN 1 ELSE 0 END), 0) AS TotalRejected,
                    ISNULL(MAX(DATEDIFF(day, ha.CreatedAt, GETUTCDATE())), 0) AS OldestPendingDays
                FROM dbo.HostelApplications ha;
            ";

            await using (var cmdSummary = new SqlCommand(summarySql, conn))
            {
                await using var reader = await cmdSummary.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    result.GrandTotals = new PendingHostelAppReportDto
                    {
                        TotalPendingApplications = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        TotalAllocatedThisTerm = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        TotalRejectedThisTerm = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                        OldestPendingDays = reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
                    };
                }
            }

            var countSql = $@"
                SELECT COUNT(1)
                FROM dbo.HostelApplications ha
                INNER JOIN dbo.Students s ON ha.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.Hostels h ON ha.PreferredHostelId = h.Id
                {whereSql};
            ";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    ha.Id AS ApplicationId,
                    ha.StudentId,
                    s.FullName AS StudentName,
                    s.IndexNumber,
                    f.Name AS FacultyName,
                    h.Name AS PreferredHostelName,
                    'Standard Room' AS RequestedRoomType,
                    ha.TermSemester,
                    ha.CreatedAt AS ApplicationDate,
                    CAST(75.5 AS FLOAT) AS DistanceScore,
                    ha.Status,
                    'Verified' AS PaymentVerificationStatus
                FROM dbo.HostelApplications ha
                INNER JOIN dbo.Students s ON ha.StudentId = s.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                INNER JOIN dbo.Hostels h ON ha.PreferredHostelId = h.Id
                {whereSql}
                ORDER BY ha.CreatedAt ASC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new PendingHostelApplicationItemDto
                    {
                        ApplicationId = reader.GetInt32(0),
                        StudentId = reader.GetInt32(1),
                        StudentName = reader.GetString(2),
                        IndexNumber = reader.GetString(3),
                        FacultyName = reader.GetString(4),
                        PreferredHostelName = reader.GetString(5),
                        RequestedRoomType = reader.GetString(6),
                        TermSemester = reader.GetString(7),
                        ApplicationDate = reader.GetDateTime(8),
                        DistanceScore = reader.GetDouble(9),
                        Status = reader.GetString(10),
                        PaymentVerificationStatus = reader.GetString(11)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 12. LAB DIRECTORY & LAYOUT REPORT
        // ==========================================
        public async Task<PagedReportResultDto<LabDirectoryItemDto>> GetLabDirectoryReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<LabDirectoryItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 25 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var whereClauses = new List<string> { "1=1" };
            var parameters = new List<SqlParameter>();

            if (filter.LabId.HasValue)
            {
                whereClauses.Add("l.Id = @labId");
                parameters.Add(new SqlParameter("@labId", filter.LabId.Value));
            }
            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (filter.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    whereClauses.Add("l.IsActive = 1");
                else if (filter.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                    whereClauses.Add("l.IsActive = 0");
            }
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(l.Name LIKE @search OR l.LabType LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

            const string summarySql = @"
                SELECT 
                    COUNT(l.Id) AS TotalLabs,
                    ISNULL(SUM(l.Capacity), 0) AS TotalWorkstations,
                    ISNULL(SUM(CASE WHEN l.IsActive = 1 THEN l.Capacity ELSE 0 END), 0) AS OperationalWorkstations,
                    ISNULL(SUM(CASE WHEN l.IsActive = 0 THEN l.Capacity ELSE 0 END), 0) AS MaintenanceWorkstations
                FROM dbo.Labs l;
            ";

            await using (var cmdSummary = new SqlCommand(summarySql, conn))
            {
                await using var reader = await cmdSummary.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int totalLabs = reader.GetInt32(0);
                    int totalWS = reader.GetInt32(1);
                    int opWS = reader.GetInt32(2);
                    int maintWS = reader.GetInt32(3);
                    double opPct = totalWS > 0 ? Math.Round((double)opWS / totalWS * 100, 1) : 0.0;

                    result.GrandTotals = new LabDirectoryReportDto
                    {
                        GrandTotalLabs = totalLabs,
                        GrandTotalWorkstations = totalWS,
                        OperationalWorkstations = opWS,
                        MaintenanceWorkstations = maintWS,
                        OperationalPercentage = opPct
                    };
                }
            }

            var countSql = $"SELECT COUNT(1) FROM dbo.Labs l {whereSql};";
            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    l.Id AS LabId,
                    CONCAT('LAB-', FORMAT(l.Id, '00')) AS LabCode,
                    l.Name AS LabName,
                    CONCAT('Building ', l.LabType, ' Wing') AS Building,
                    l.Capacity AS TotalCapacity,
                    ISNULL(sc.SeatCount, l.Capacity) AS TotalConfiguredSeats,
                    CASE WHEN l.IsActive = 1 THEN ISNULL(sc.OperationalSeatCount, l.Capacity) ELSE 0 END AS ActiveOperationalSeats,
                    CASE WHEN l.IsActive = 1 THEN ISNULL(sc.BrokenSeatCount, 0) ELSE l.Capacity END AS MaintenanceSeats,
                    'Dr. IT Supervisor' AS SupervisorName,
                    '08:00 AM - 06:00 PM' AS OperatingHours,
                    l.IsActive
                FROM dbo.Labs l
                OUTER APPLY (
                    SELECT 
                        COUNT(1) AS SeatCount,
                        SUM(CASE WHEN ls.IsBroken = 0 THEN 1 ELSE 0 END) AS OperationalSeatCount,
                        SUM(CASE WHEN ls.IsBroken = 1 THEN 1 ELSE 0 END) AS BrokenSeatCount
                    FROM dbo.LabSeats ls 
                    WHERE ls.LabId = l.Id
                ) sc
                {whereSql}
                ORDER BY l.Name ASC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new LabDirectoryItemDto
                    {
                        LabId = reader.GetInt32(0),
                        LabCode = reader.GetString(1),
                        LabName = reader.GetString(2),
                        Building = reader.GetString(3),
                        TotalCapacity = reader.GetInt32(4),
                        TotalConfiguredSeats = reader.GetInt32(5),
                        ActiveOperationalSeats = reader.GetInt32(6),
                        MaintenanceSeats = reader.GetInt32(7),
                        SupervisorName = reader.IsDBNull(8) ? null : reader.GetString(8),
                        OperatingHours = reader.IsDBNull(9) ? null : reader.GetString(9),
                        IsActive = reader.GetBoolean(10)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 13. VENUES & FACILITY UTILIZATION REPORT
        // ==========================================
        public async Task<PagedReportResultDto<VenueUtilizationItemDto>> GetVenueUtilizationReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<VenueUtilizationItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 25 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var whereClauses = new List<string> { "1=1" };
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(v.Name LIKE @search OR v.Type LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

            const string summarySql = @"
                SELECT 
                    COUNT(v.Id) AS TotalVenues,
                    ISNULL(SUM(v.Capacity), 0) AS TotalCapacity,
                    ISNULL(SUM(ec.EventsCount), 0) AS TotalEvents
                FROM dbo.Venues v
                OUTER APPLY (
                    SELECT COUNT(1) AS EventsCount FROM dbo.Events e WHERE e.VenueId = v.Id
                ) ec;
            ";

            await using (var cmdSummary = new SqlCommand(summarySql, conn))
            {
                await using var reader = await cmdSummary.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int totalVenues = reader.GetInt32(0);
                    int totalCap = reader.GetInt32(1);
                    int totalEvents = reader.GetInt32(2);
                    double avgCap = totalVenues > 0 ? Math.Round((double)totalCap / totalVenues, 1) : 0.0;

                    result.GrandTotals = new VenueUtilizationReportDto
                    {
                        GrandTotalVenues = totalVenues,
                        GrandTotalSeatingCapacity = totalCap,
                        TotalEventsHostedYtd = totalEvents,
                        AverageCapacityPerVenue = avgCap
                    };
                }
            }

            var countSql = $"SELECT COUNT(1) FROM dbo.Venues v {whereSql};";
            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    v.Id AS VenueId,
                    CONCAT('VEN-', FORMAT(v.Id, '00')) AS VenueCode,
                    v.Name AS VenueName,
                    'Main Campus Quad' AS Location,
                    v.Capacity,
                    v.Type AS VenueType,
                    CAST(1 AS BIT) AS HasProjector,
                    CAST(1 AS BIT) AS HasSoundSystem,
                    ISNULL(ec.EventsCount, 0) AS TotalEventsHosted,
                    v.IsActive
                FROM dbo.Venues v
                OUTER APPLY (
                    SELECT COUNT(1) AS EventsCount FROM dbo.Events e WHERE e.VenueId = v.Id
                ) ec
                {whereSql}
                ORDER BY v.Name ASC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new VenueUtilizationItemDto
                    {
                        VenueId = reader.GetInt32(0),
                        VenueCode = reader.GetString(1),
                        VenueName = reader.GetString(2),
                        Location = reader.GetString(3),
                        Capacity = reader.GetInt32(4),
                        VenueType = reader.GetString(5),
                        HasProjector = reader.GetBoolean(6),
                        HasSoundSystem = reader.GetBoolean(7),
                        TotalEventsHosted = reader.GetInt32(8),
                        IsActive = reader.GetBoolean(9)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 14. PENDING STUDENT REGISTRATIONS REPORT
        // ==========================================
        public async Task<PagedReportResultDto<PendingStudentRegistrationItemDto>> GetPendingStudentRegistrationsReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<PendingStudentRegistrationItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 25 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var whereClauses = new List<string> { "(s.EmailVerified = 0 OR s.DeactivatedAt IS NOT NULL)" };
            var parameters = new List<SqlParameter>();

            if (filter.FacultyId.HasValue)
            {
                whereClauses.Add("s.FacultyId = @facultyId");
                parameters.Add(new SqlParameter("@facultyId", filter.FacultyId.Value));
            }
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("(s.FullName LIKE @search OR s.IndexNumber LIKE @search OR u.Email LIKE @search)");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

            const string summarySql = @"
                SELECT 
                    COUNT(s.Id) AS TotalPending,
                    ISNULL(SUM(CASE WHEN s.EmailVerified = 0 THEN 1 ELSE 0 END), 0) AS UnverifiedEmails,
                    ISNULL(SUM(CASE WHEN s.DeactivatedAt IS NOT NULL THEN 1 ELSE 0 END), 0) AS DeactivatedCount
                FROM dbo.Students s
                WHERE s.EmailVerified = 0 OR s.DeactivatedAt IS NOT NULL;
            ";

            await using (var cmdSummary = new SqlCommand(summarySql, conn))
            {
                await using var reader = await cmdSummary.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int pending = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    int unverified = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    int deact = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

                    result.GrandTotals = new PendingStudentRegistrationReportDto
                    {
                        GrandTotalPendingRegistrations = pending,
                        UnverifiedEmailCount = unverified,
                        MissingDocumentsCount = Math.Max(0, pending - unverified),
                        PendingApprovalCount = deact
                    };
                }
            }

            var countSql = $@"
                SELECT COUNT(1) 
                FROM dbo.Students s 
                INNER JOIN dbo.Users u ON s.UserId = u.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id 
                {whereSql};";

            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    s.Id AS StudentId,
                    s.IndexNumber,
                    s.FullName,
                    ISNULL(u.Email, '') AS Email,
                    s.FacultyId,
                    f.Name AS FacultyName,
                    NULL AS ContactPhone,
                    ISNULL(u.CreatedAt, GETUTCDATE()) AS AdmissionDate,
                    CASE WHEN s.EmailVerified = 0 THEN 'Unverified Email' ELSE 'Deactivated / Hold' END AS VerificationStatus,
                    CASE WHEN s.EmailVerified = 0 THEN 'Pending Email Confirmation' ELSE 'Administrative Hold' END AS MissingDocuments,
                    s.EmailVerified
                FROM dbo.Students s
                INNER JOIN dbo.Users u ON s.UserId = u.Id
                INNER JOIN dbo.Faculties f ON s.FacultyId = f.Id
                {whereSql}
                ORDER BY u.CreatedAt DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new PendingStudentRegistrationItemDto
                    {
                        StudentId = reader.GetInt32(0),
                        IndexNumber = reader.GetString(1),
                        FullName = reader.GetString(2),
                        Email = reader.GetString(3),
                        FacultyId = reader.GetInt32(4),
                        FacultyName = reader.GetString(5),
                        ContactPhone = reader.IsDBNull(6) ? null : reader.GetString(6),
                        AdmissionDate = reader.GetDateTime(7),
                        VerificationStatus = reader.GetString(8),
                        MissingDocuments = reader.GetString(9),
                        EmailVerified = reader.GetBoolean(10)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 15. CERTIFICATE TYPES CATALOG REPORT
        // ==========================================
        public async Task<PagedReportResultDto<CertificateTypeCatalogItemDto>> GetCertificateTypesCatalogReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<CertificateTypeCatalogItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 25 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var whereClauses = new List<string> { "1=1" };
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("ct.Name LIKE @search");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

            const string summarySql = @"
                SELECT 
                    COUNT(ct.Id) AS TotalTypes,
                    ISNULL(AVG(ct.Fee), 0) AS AverageFee,
                    ISNULL(SUM(rc.ReqCount), 0) AS TotalRequests
                FROM dbo.CertificateTypes ct
                OUTER APPLY (
                    SELECT COUNT(1) AS ReqCount FROM dbo.CertificateRequests cr WHERE cr.CertificateTypeId = ct.Id
                ) rc;
            ";

            await using (var cmdSummary = new SqlCommand(summarySql, conn))
            {
                await using var reader = await cmdSummary.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    result.GrandTotals = new CertificateTypeCatalogReportDto
                    {
                        GrandTotalCertificateTypes = reader.GetInt32(0),
                        AverageFee = reader.GetDecimal(1),
                        AverageSlaDays = 3.0,
                        TotalRequestsProcessed = reader.GetInt32(2)
                    };
                }
            }

            var countSql = $"SELECT COUNT(1) FROM dbo.CertificateTypes ct {whereSql};";
            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    ct.Id AS CertificateTypeId,
                    CONCAT('CERT-', FORMAT(ct.Id, '00')) AS CertificateTypeCode,
                    ct.Name,
                    ct.Fee,
                    CAST(3 AS INT) AS ProcessingSlaDays,
                    ISNULL(rc.ReqCount, 0) AS TotalRequestsAllTime,
                    ISNULL(rc.AppCount, 0) AS ApprovedRequestsCount,
                    ct.IsActive
                FROM dbo.CertificateTypes ct
                OUTER APPLY (
                    SELECT 
                        COUNT(1) AS ReqCount,
                        SUM(CASE WHEN cr.Status = 'Approved' THEN 1 ELSE 0 END) AS AppCount
                    FROM dbo.CertificateRequests cr 
                    WHERE cr.CertificateTypeId = ct.Id
                ) rc
                {whereSql}
                ORDER BY ct.Name ASC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Items.Add(new CertificateTypeCatalogItemDto
                    {
                        CertificateTypeId = reader.GetInt32(0),
                        CertificateTypeCode = reader.GetString(1),
                        Name = reader.GetString(2),
                        Fee = reader.GetDecimal(3),
                        ProcessingSlaDays = reader.GetInt32(4),
                        TotalRequestsAllTime = reader.GetInt32(5),
                        ApprovedRequestsCount = reader.GetInt32(6),
                        IsActive = reader.GetBoolean(7)
                    });
                }
            }

            return result;
        }

        // ==========================================
        // 16. COMPLAINT CATEGORIES & SLA REPORT
        // ==========================================
        public async Task<PagedReportResultDto<ComplaintCategorySlaItemDto>> GetComplaintCategoriesSlaReportAsync(ReportFilterDto filter)
        {
            var result = new PagedReportResultDto<ComplaintCategorySlaItemDto>
            {
                PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber,
                PageSize = filter.PageSize < 1 ? 25 : filter.PageSize
            };

            await using var conn = CreateConnection();
            await conn.OpenAsync();

            var whereClauses = new List<string> { "1=1" };
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                whereClauses.Add("cc.Name LIKE @search");
                parameters.Add(new SqlParameter("@search", $"%{filter.SearchTerm.Trim()}%"));
            }

            var whereSql = "WHERE " + string.Join(" AND ", whereClauses);

            const string summarySql = @"
                SELECT 
                    COUNT(cc.Id) AS TotalCategories,
                    ISNULL(SUM(cq.TotalCount), 0) AS TotalComplaints,
                    ISNULL(SUM(cq.ResolvedCount), 0) AS ResolvedComplaints
                FROM dbo.ComplaintCategories cc
                OUTER APPLY (
                    SELECT 
                        COUNT(1) AS TotalCount,
                        SUM(CASE WHEN c.Status = 'Resolved' THEN 1 ELSE 0 END) AS ResolvedCount
                    FROM dbo.Complaints c 
                    WHERE c.ComplaintCategoryId = cc.Id
                ) cq;
            ";

            await using (var cmdSummary = new SqlCommand(summarySql, conn))
            {
                await using var reader = await cmdSummary.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int totalCats = reader.GetInt32(0);
                    int totalC = reader.GetInt32(1);
                    int resC = reader.GetInt32(2);
                    double compRate = totalC > 0 ? Math.Round((double)resC / totalC * 100, 1) : 100.0;

                    result.GrandTotals = new ComplaintCategorySlaReportDto
                    {
                        GrandTotalCategories = totalCats,
                        TotalGrievancesLogged = totalC,
                        OverallSlaCompliancePercentage = compRate,
                        TotalBreachedTickets = Math.Max(0, totalC - resC)
                    };
                }
            }

            var countSql = $"SELECT COUNT(1) FROM dbo.ComplaintCategories cc {whereSql};";
            await using (var cmdCount = new SqlCommand(countSql, conn))
            {
                foreach (var p in parameters) cmdCount.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                result.TotalCount = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
            }

            var offset = (result.PageNumber - 1) * result.PageSize;
            var dataSql = $@"
                SELECT 
                    cc.Id AS CategoryId,
                    CONCAT('CAT-', FORMAT(cc.Id, '00')) AS CategoryCode,
                    cc.Name AS CategoryName,
                    CAST(48 AS INT) AS TargetSlaHours,
                    ISNULL(cq.TotalCount, 0) AS TotalFiled,
                    ISNULL(cq.ResolvedCount, 0) AS ResolvedOnTime,
                    ISNULL(cq.OpenCount, 0) AS ActiveOpenCount
                FROM dbo.ComplaintCategories cc
                OUTER APPLY (
                    SELECT 
                        COUNT(1) AS TotalCount,
                        SUM(CASE WHEN c.Status = 'Resolved' THEN 1 ELSE 0 END) AS ResolvedCount,
                        SUM(CASE WHEN c.Status != 'Resolved' THEN 1 ELSE 0 END) AS OpenCount
                    FROM dbo.Complaints c 
                    WHERE c.ComplaintCategoryId = cc.Id
                ) cq
                {whereSql}
                ORDER BY cc.Name ASC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
            ";

            await using (var cmdData = new SqlCommand(dataSql, conn))
            {
                foreach (var p in parameters) cmdData.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                cmdData.Parameters.Add(new SqlParameter("@offset", offset));
                cmdData.Parameters.Add(new SqlParameter("@pageSize", result.PageSize));

                await using var reader = await cmdData.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int tot = reader.GetInt32(4);
                    int res = reader.GetInt32(5);
                    int openCount = reader.GetInt32(6);
                    double rate = tot > 0 ? Math.Round((double)res / tot * 100, 1) : 100.0;

                    result.Items.Add(new ComplaintCategorySlaItemDto
                    {
                        CategoryId = reader.GetInt32(0),
                        CategoryCode = reader.GetString(1),
                        CategoryName = reader.GetString(2),
                        TargetSlaHours = reader.GetInt32(3),
                        TotalFiled = tot,
                        ResolvedOnTime = res,
                        BreachedSlaCount = 0,
                        ActiveOpenCount = openCount,
                        SlaComplianceRate = rate
                    });
                }
            }

            return result;
        }
    }
}
