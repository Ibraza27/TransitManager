using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestClientFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestEmail",
                table: "Quotes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "Quotes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "Quotes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestEmail",
                table: "Invoices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                table: "Invoices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                table: "Invoices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2026, 1, 30, 17, 32, 48, 207, DateTimeKind.Utc).AddTicks(6958));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestEmail",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "GuestEmail",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "GuestName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                table: "Invoices");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2026, 1, 25, 18, 0, 47, 460, DateTimeKind.Utc).AddTicks(6097));
        }
    }
}
