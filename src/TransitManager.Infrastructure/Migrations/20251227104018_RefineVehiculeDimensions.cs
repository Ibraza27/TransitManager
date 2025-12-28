using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefineVehiculeDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DimensionsHauteurCm",
                table: "Vehicules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DimensionsLargeurCm",
                table: "Vehicules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DimensionsLongueurCm",
                table: "Vehicules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPriceCalculated",
                table: "Vehicules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 27, 10, 40, 15, 181, DateTimeKind.Utc).AddTicks(7898));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DimensionsHauteurCm",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "DimensionsLargeurCm",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "DimensionsLongueurCm",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "IsPriceCalculated",
                table: "Vehicules");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 26, 20, 52, 33, 134, DateTimeKind.Utc).AddTicks(583));
        }
    }
}
