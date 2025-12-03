using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentsAndVehiculeFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessoiresJson",
                table: "Vehicules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateEtatDesLieux",
                table: "Vehicules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LieuEtatDesLieux",
                table: "Vehicules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureAgent",
                table: "Vehicules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureClient",
                table: "Vehicules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehiculeId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 2, 16, 25, 52, 784, DateTimeKind.Utc).AddTicks(2822));

            migrationBuilder.CreateIndex(
                name: "IX_Documents_VehiculeId",
                table: "Documents",
                column: "VehiculeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Vehicules_VehiculeId",
                table: "Documents",
                column: "VehiculeId",
                principalTable: "Vehicules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Vehicules_VehiculeId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_VehiculeId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "AccessoiresJson",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "DateEtatDesLieux",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "LieuEtatDesLieux",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "SignatureAgent",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "SignatureClient",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "VehiculeId",
                table: "Documents");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 11, 13, 19, 40, 23, 770, DateTimeKind.Utc).AddTicks(2183));
        }
    }
}
