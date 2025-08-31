using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaiementToColisRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ColisId",
                table: "Paiements",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 8, 31, 7, 56, 42, 830, DateTimeKind.Utc).AddTicks(7021), "$2a$11$KtVGUhdBS/t.2qWzkGF/teYulcjhGu6gJRxzcXUMTRD.44hElEZxK" });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ColisId",
                table: "Paiements",
                column: "ColisId");

            migrationBuilder.AddForeignKey(
                name: "FK_Paiements_Colis_ColisId",
                table: "Paiements",
                column: "ColisId",
                principalTable: "Colis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Paiements_Colis_ColisId",
                table: "Paiements");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ColisId",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "ColisId",
                table: "Paiements");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 8, 27, 21, 36, 38, 895, DateTimeKind.Utc).AddTicks(8319), "$2a$11$47CimAPLqf80X5ildRmPXuC0TWgjvHAIA7CeifbveROmjA1zR0dOu" });
        }
    }
}
