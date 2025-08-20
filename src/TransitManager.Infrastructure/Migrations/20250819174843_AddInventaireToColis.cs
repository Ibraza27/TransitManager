using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventaireToColis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InventaireJson",
                table: "Colis",
                type: "jsonb",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 8, 19, 17, 48, 42, 354, DateTimeKind.Utc).AddTicks(5571), "$2a$11$P1ZxkI.jbS5b3E6yKDjHcO5asaO8FcVsN8g9C56SDQ5AynfOxnnFC" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventaireJson",
                table: "Colis");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 8, 16, 13, 51, 5, 480, DateTimeKind.Utc).AddTicks(7797), "$2a$11$lGoMpMpgQju.YkYKz3JoAOziP6xFd/wS5BA0snlEi4a7F8uHsaFEq" });
        }
    }
}
