using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAddressToColis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdresseLivraison",
                table: "Colis",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 9, 14, 6, 15, 48, 922, DateTimeKind.Utc).AddTicks(6415), "$2a$11$pTdOiicmME4EAKuc4aBQmOJIhqgHXmPYhQBzIViauCL2ggavYWYu." });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdresseLivraison",
                table: "Colis");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 9, 8, 19, 55, 13, 976, DateTimeKind.Utc).AddTicks(2744), "$2a$11$T5rYK1C0WMtXwB6/WPpCBeObp2N4mjbmKmZjer8Z.0cu8NKy556ja" });
        }
    }
}
