using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressFieldsToColisVehicule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdresseDestination",
                table: "Vehicules",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdresseFrance",
                table: "Vehicules",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdresseDestination",
                table: "Colis",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdresseFrance",
                table: "Colis",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 23, 21, 47, 50, 878, DateTimeKind.Utc).AddTicks(3132));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdresseDestination",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "AdresseFrance",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "AdresseDestination",
                table: "Colis");

            migrationBuilder.DropColumn(
                name: "AdresseFrance",
                table: "Colis");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 15, 21, 10, 39, 343, DateTimeKind.Utc).AddTicks(6270));
        }
    }
}
