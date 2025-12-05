using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColisInventorySignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateSignatureInventaire",
                table: "Colis",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LieuSignatureInventaire",
                table: "Colis",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureClientInventaire",
                table: "Colis",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 5, 19, 38, 3, 311, DateTimeKind.Utc).AddTicks(6176));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateSignatureInventaire",
                table: "Colis");

            migrationBuilder.DropColumn(
                name: "LieuSignatureInventaire",
                table: "Colis");

            migrationBuilder.DropColumn(
                name: "SignatureClientInventaire",
                table: "Colis");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 2, 16, 25, 52, 784, DateTimeKind.Utc).AddTicks(2822));
        }
    }
}
