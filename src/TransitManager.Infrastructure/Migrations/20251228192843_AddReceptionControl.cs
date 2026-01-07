using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceptionControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ColisId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehiculeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsValidated = table.Column<bool>(type: "boolean", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RateService = table.Column<int>(type: "integer", nullable: false),
                    RateCondition = table.Column<int>(type: "integer", nullable: false),
                    RateCommunication = table.Column<int>(type: "integer", nullable: false),
                    RateTimeframe = table.Column<int>(type: "integer", nullable: false),
                    RateRecommendation = table.Column<int>(type: "integer", nullable: false),
                    CommentService = table.Column<string>(type: "text", nullable: true),
                    CommentCondition = table.Column<string>(type: "text", nullable: true),
                    CommentCommunication = table.Column<string>(type: "text", nullable: true),
                    CommentTimeframe = table.Column<string>(type: "text", nullable: true),
                    CommentRecommendation = table.Column<string>(type: "text", nullable: true),
                    GlobalComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceptionControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceptionControls_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceptionControls_Colis_ColisId",
                        column: x => x.ColisId,
                        principalTable: "Colis",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReceptionControls_Vehicules_VehiculeId",
                        column: x => x.VehiculeId,
                        principalTable: "Vehicules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReceptionIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceptionControlId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    InventoryItemName = table.Column<string>(type: "text", nullable: true),
                    DeclaredValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    X = table.Column<double>(type: "double precision", nullable: true),
                    Y = table.Column<double>(type: "double precision", nullable: true),
                    PhotoIds = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceptionIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceptionIssues_ReceptionControls_ReceptionControlId",
                        column: x => x.ReceptionControlId,
                        principalTable: "ReceptionControls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 28, 19, 28, 40, 616, DateTimeKind.Utc).AddTicks(2000));

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionControls_ClientId",
                table: "ReceptionControls",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionControls_ColisId",
                table: "ReceptionControls",
                column: "ColisId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionControls_VehiculeId",
                table: "ReceptionControls",
                column: "VehiculeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptionIssues_ReceptionControlId",
                table: "ReceptionIssues",
                column: "ReceptionControlId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceptionIssues");

            migrationBuilder.DropTable(
                name: "ReceptionControls");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 27, 18, 30, 41, 614, DateTimeKind.Utc).AddTicks(7408));
        }
    }
}
