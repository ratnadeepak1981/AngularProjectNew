using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CampusServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class Addcontraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ComplaintCategories",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, true, "Hostel Room Maintenance" },
                    { 2, true, "Network WiFi Interruption" }
                });

            migrationBuilder.InsertData(
                table: "Faculties",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, true, "Faculty of Computing" },
                    { 2, true, "Faculty of Engineering" },
                    { 3, true, "Faculty of Business Management" }
                });

            migrationBuilder.InsertData(
                table: "FeeTypes",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, true, "Tuition Fee" },
                    { 2, true, "Lab Fine / Equipment Fee" },
                    { 3, true, "Hostel Accommodation Fee" },
                    { 4, true, "Library Fine & Late Return" },
                    { 5, true, "Student Identity Card Renewal Fee" }
                });

            migrationBuilder.InsertData(
                table: "Labs",
                columns: new[] { "Id", "Capacity", "IsActive", "LabType", "Name", "TotalColumns", "TotalRows" },
                values: new object[,]
                {
                    { 1, 0, true, "Computer", "Computer Lab 1", null, null },
                    { 2, 0, true, "Computer", "Computer Lab 2", null, null }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "SettingKey", "SettingValue" },
                values: new object[,]
                {
                    { "LabBookingHoldMinutes", "15" },
                    { "LabBookingSlotDurationMinutes", "15" },
                    { "MaxDailySlots", "2" },
                    { "MaxLabBookingsPerStudentPerDay", "2" }
                });

            migrationBuilder.InsertData(
                table: "LabBookingTimeSlots",
                columns: new[] { "Id", "CreatedAt", "DisplayOrder", "EndTime", "IsActive", "LabId", "StartTime", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 10, 11, 37, 45, 641, DateTimeKind.Utc).AddTicks(8841), 1, "11:00", true, 1, "09:00", null },
                    { 2, new DateTime(2026, 9, 10, 11, 37, 45, 641, DateTimeKind.Utc).AddTicks(8844), 2, "13:00", true, 1, "11:00", null },
                    { 3, new DateTime(2026, 9, 10, 11, 37, 45, 641, DateTimeKind.Utc).AddTicks(8845), 3, "16:00", true, 1, "14:00", null },
                    { 4, new DateTime(2026, 9, 10, 11, 37, 45, 641, DateTimeKind.Utc).AddTicks(8846), 4, "18:00", true, 1, "16:00", null },
                    { 5, new DateTime(2026, 9, 10, 11, 37, 45, 641, DateTimeKind.Utc).AddTicks(8847), 1, "11:00", true, 2, "09:00", null },
                    { 6, new DateTime(2026, 9, 10, 11, 37, 45, 641, DateTimeKind.Utc).AddTicks(8848), 2, "13:00", true, 2, "11:00", null },
                    { 7, new DateTime(2026, 9, 10, 11, 37, 45, 641, DateTimeKind.Utc).AddTicks(8849), 3, "16:00", true, 2, "14:00", null },
                    { 8, new DateTime(2026, 9, 10, 11, 37, 45, 641, DateTimeKind.Utc).AddTicks(8850), 4, "18:00", true, 2, "16:00", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
