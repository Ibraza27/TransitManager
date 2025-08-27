using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameBalanceTotalToImpayesOnClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientId1",
                table: "Vehicules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceTotal1",
                table: "Clients",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "NombreConteneursUniques",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 8, 26, 21, 35, 48, 532, DateTimeKind.Utc).AddTicks(1155), "$2a$11$s.OXcLZkodB0lQNZDJFlP.n4dVo3He66.G/aruyNrRGdXF5zqI.IS" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_ClientId1",
                table: "Vehicules",
                column: "ClientId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicules_Clients_ClientId1",
                table: "Vehicules",
                column: "ClientId1",
                principalTable: "Clients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicules_Clients_ClientId1",
                table: "Vehicules");

            migrationBuilder.DropIndex(
                name: "IX_Vehicules_ClientId1",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "ClientId1",
                table: "Vehicules");

            migrationBuilder.DropColumn(
                name: "BalanceTotal1",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "NombreConteneursUniques",
                table: "Clients");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "DateCreation", "MotDePasseHash" },
                values: new object[] { new DateTime(2025, 8, 19, 17, 48, 42, 354, DateTimeKind.Utc).AddTicks(5571), "$2a$11$P1ZxkI.jbS5b3E6yKDjHcO5asaO8FcVsN8g9C56SDQ5AynfOxnnFC" });
        }
    }
}
