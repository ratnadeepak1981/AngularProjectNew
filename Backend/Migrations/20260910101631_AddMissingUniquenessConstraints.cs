using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingUniquenessConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rooms_HostelId",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Events_VenueId",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "StudentPhoneNumbers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 10, 16, 30, 988, DateTimeKind.Utc).AddTicks(2718));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 10, 16, 30, 988, DateTimeKind.Utc).AddTicks(2721));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 10, 16, 30, 988, DateTimeKind.Utc).AddTicks(2723));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 10, 16, 30, 988, DateTimeKind.Utc).AddTicks(2724));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 10, 16, 30, 988, DateTimeKind.Utc).AddTicks(2725));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 10, 16, 30, 988, DateTimeKind.Utc).AddTicks(2726));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 10, 16, 30, 988, DateTimeKind.Utc).AddTicks(2727));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 10, 16, 30, 988, DateTimeKind.Utc).AddTicks(2728));

            migrationBuilder.CreateIndex(
                name: "IX_Venues_Name",
                table: "Venues",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentPhoneNumbers_PhoneNumber",
                table: "StudentPhoneNumbers",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Rooms_Hostel_RoomNumber",
                table: "Rooms",
                columns: new[] { "HostelId", "RoomNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Labs_Name",
                table: "Labs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hostels_Name",
                table: "Hostels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeTypes_Name",
                table: "FeeTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Events_Venue_Schedule",
                table: "Events",
                columns: new[] { "VenueId", "StartDateTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venues_Name",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_StudentPhoneNumbers_PhoneNumber",
                table: "StudentPhoneNumbers");

            migrationBuilder.DropIndex(
                name: "UX_Rooms_Hostel_RoomNumber",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Labs_Name",
                table: "Labs");

            migrationBuilder.DropIndex(
                name: "IX_Hostels_Name",
                table: "Hostels");

            migrationBuilder.DropIndex(
                name: "IX_FeeTypes_Name",
                table: "FeeTypes");

            migrationBuilder.DropIndex(
                name: "UX_Events_Venue_Schedule",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "StudentPhoneNumbers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 5, 48, 34, 4, DateTimeKind.Utc).AddTicks(7523));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 5, 48, 34, 4, DateTimeKind.Utc).AddTicks(7531));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 5, 48, 34, 4, DateTimeKind.Utc).AddTicks(7532));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 5, 48, 34, 4, DateTimeKind.Utc).AddTicks(7534));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 5, 48, 34, 4, DateTimeKind.Utc).AddTicks(7535));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 5, 48, 34, 4, DateTimeKind.Utc).AddTicks(7537));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 5, 48, 34, 4, DateTimeKind.Utc).AddTicks(7538));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 10, 5, 48, 34, 4, DateTimeKind.Utc).AddTicks(7539));

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HostelId",
                table: "Rooms",
                column: "HostelId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_VenueId",
                table: "Events",
                column: "VenueId");
        }
    }
}
