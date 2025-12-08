using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateExpirationTokenEmail",
                table: "Utilisateurs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirme",
                table: "Utilisateurs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TokenVerificationEmail",
                table: "Utilisateurs",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "DateExpirationTokenEmail", "EmailConfirme", "TokenVerificationEmail" },
                values: new object[] { new DateTime(2025, 12, 7, 15, 38, 58, 997, DateTimeKind.Utc).AddTicks(9725), null, false, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateExpirationTokenEmail",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "EmailConfirme",
                table: "Utilisateurs");

            migrationBuilder.DropColumn(
                name: "TokenVerificationEmail",
                table: "Utilisateurs");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 5, 19, 38, 3, 311, DateTimeKind.Utc).AddTicks(6176));
        }
    }
}
