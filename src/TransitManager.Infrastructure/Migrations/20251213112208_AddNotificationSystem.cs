using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_DateCreation",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_EstLue",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_Type",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UtilisateurId_EstLue",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActionParametre",
                table: "Notifications");

            migrationBuilder.AddColumn<string>(
                name: "Categorie",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Couleur",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Icone",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 13, 11, 22, 5, 631, DateTimeKind.Utc).AddTicks(6949));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categorie",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Couleur",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Icone",
                table: "Notifications");

            migrationBuilder.AddColumn<string>(
                name: "ActionParametre",
                table: "Notifications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 11, 22, 18, 36, 761, DateTimeKind.Utc).AddTicks(3990));

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DateCreation",
                table: "Notifications",
                column: "DateCreation");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EstLue",
                table: "Notifications",
                column: "EstLue");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UtilisateurId_EstLue",
                table: "Notifications",
                columns: new[] { "UtilisateurId", "EstLue" });
        }
    }
}
