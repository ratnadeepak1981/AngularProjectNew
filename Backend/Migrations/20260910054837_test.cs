using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusServicesPortal.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 12, 5, 48, 347, DateTimeKind.Utc).AddTicks(756));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 12, 5, 48, 347, DateTimeKind.Utc).AddTicks(759));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 12, 5, 48, 347, DateTimeKind.Utc).AddTicks(760));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 12, 5, 48, 347, DateTimeKind.Utc).AddTicks(761));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 12, 5, 48, 347, DateTimeKind.Utc).AddTicks(763));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 12, 5, 48, 347, DateTimeKind.Utc).AddTicks(764));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 12, 5, 48, 347, DateTimeKind.Utc).AddTicks(765));

            migrationBuilder.UpdateData(
                table: "LabBookingTimeSlots",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 9, 9, 12, 5, 48, 347, DateTimeKind.Utc).AddTicks(766));
        }
    }
}
