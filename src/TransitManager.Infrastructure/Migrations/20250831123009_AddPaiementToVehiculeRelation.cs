using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaiementToVehiculeRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VehiculeId",
                table: "Paiements",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 8, 31, 12, 30, 8, 128, DateTimeKind.Utc).AddTicks(8538), "$2a$11$VTMnL3dQeybXXp7SV07d/u8zMhCh08mbNcf7hEyeSQiNBOaeIziIK" });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_VehiculeId",
                table: "Paiements",
                column: "VehiculeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Paiements_Vehicules_VehiculeId",
                table: "Paiements",
                column: "VehiculeId",
                principalTable: "Vehicules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Paiements_Vehicules_VehiculeId",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_VehiculeId",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "VehiculeId",
                table: "Paiements");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 8, 31, 7, 56, 42, 830, DateTimeKind.Utc).AddTicks(7021), "$2a$11$KtVGUhdBS/t.2qWzkGF/teYulcjhGu6gJRxzcXUMTRD.44hElEZxK" });
        }
    }
}
