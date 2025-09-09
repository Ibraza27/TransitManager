using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateColisWithVolume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hauteur",
                table: "Colis");

            migrationBuilder.DropColumn(
                name: "Largeur",
                table: "Colis");

            migrationBuilder.DropColumn(
                name: "Longueur",
                table: "Colis");

            migrationBuilder.RenameColumn(
                name: "Poids",
                table: "Colis",
                newName: "Volume");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 9, 8, 19, 55, 13, 976, DateTimeKind.Utc).AddTicks(2744), "$2a$11$T5rYK1C0WMtXwB6/WPpCBeObp2N4mjbmKmZjer8Z.0cu8NKy556ja" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Volume",
                table: "Colis",
                newName: "Poids");

            migrationBuilder.AddColumn<decimal>(
                name: "Hauteur",
                table: "Colis",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Largeur",
                table: "Colis",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longueur",
                table: "Colis",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 8, 31, 12, 30, 8, 128, DateTimeKind.Utc).AddTicks(8538), "$2a$11$VTMnL3dQeybXXp7SV07d/u8zMhCh08mbNcf7hEyeSQiNBOaeIziIK" });
        }
    }
}
