using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConteneurMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConteneurId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 11, 22, 18, 36, 761, DateTimeKind.Utc).AddTicks(3990));

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConteneurId",
                table: "Messages",
                column: "ConteneurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Conteneurs_ConteneurId",
                table: "Messages",
                column: "ConteneurId",
                principalTable: "Conteneurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Conteneurs_ConteneurId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ConteneurId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ConteneurId",
                table: "Messages");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 9, 18, 43, 8, 559, DateTimeKind.Utc).AddTicks(6195));
        }
    }
}
