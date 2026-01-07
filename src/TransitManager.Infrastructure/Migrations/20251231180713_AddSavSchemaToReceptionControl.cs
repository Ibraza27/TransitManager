using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavSchemaToReceptionControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SavSchemaPointsJson",
                table: "ReceptionControls",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavSchemaStrokesJson",
                table: "ReceptionControls",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 31, 18, 7, 11, 435, DateTimeKind.Utc).AddTicks(4206));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SavSchemaPointsJson",
                table: "ReceptionControls");

            migrationBuilder.DropColumn(
                name: "SavSchemaStrokesJson",
                table: "ReceptionControls");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 28, 19, 28, 40, 616, DateTimeKind.Utc).AddTicks(2000));
        }
    }
}
