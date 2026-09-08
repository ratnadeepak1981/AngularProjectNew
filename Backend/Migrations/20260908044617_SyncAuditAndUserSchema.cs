using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class SyncAuditAndUserSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LabBookings_SeatId",
                table: "LabBookings");

            migrationBuilder.DropIndex(
                name: "IX_LabBookings_StudentId",
                table: "LabBookings");

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndUtc",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TemporaryPasswordExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BeforeValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentMasterLists_FacultyId",
                table: "StudentMasterLists",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "UX_LabBookings_Seat_ActiveSlot",
                table: "LabBookings",
                columns: new[] { "SeatId", "BookingDate", "TimeSlot" },
                unique: true,
                filter: "[Status] = 'Confirmed'");

            migrationBuilder.CreateIndex(
                name: "UX_LabBookings_Student_ActiveSlot",
                table: "LabBookings",
                columns: new[] { "StudentId", "BookingDate", "TimeSlot" },
                unique: true,
                filter: "[Status] = 'Confirmed'");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsReviewed_IsSuccess",
                table: "AuditLogs",
                columns: new[] { "IsReviewed", "IsSuccess" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Module_Action",
                table: "AuditLogs",
                columns: new[] { "Module", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentMasterLists_Faculties_FacultyId",
                table: "StudentMasterLists",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentMasterLists_Faculties_FacultyId",
                table: "StudentMasterLists");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_StudentMasterLists_FacultyId",
                table: "StudentMasterLists");

            migrationBuilder.DropIndex(
                name: "UX_LabBookings_Seat_ActiveSlot",
                table: "LabBookings");

            migrationBuilder.DropIndex(
                name: "UX_LabBookings_Student_ActiveSlot",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEndUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TemporaryPasswordExpiresAt",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_LabBookings_SeatId",
                table: "LabBookings",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_LabBookings_StudentId",
                table: "LabBookings",
                column: "StudentId");
        }
    }
}
