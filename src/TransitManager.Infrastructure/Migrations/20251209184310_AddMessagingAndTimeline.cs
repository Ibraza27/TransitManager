using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingAndTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelatedEntityId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedEntityType",
                table: "Notifications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    DateEnvoi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpediteurId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColisId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehiculeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    DateLecture = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Colis_ColisId",
                        column: x => x.ColisId,
                        principalTable: "Colis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Messages_Utilisateurs_ExpediteurId",
                        column: x => x.ExpediteurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Messages_Vehicules_VehiculeId",
                        column: x => x.VehiculeId,
                        principalTable: "Vehicules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DescriptionPublique = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ColisId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehiculeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConteneurId = table.Column<Guid>(type: "uuid", nullable: true),
                    Statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingEvents_Colis_ColisId",
                        column: x => x.ColisId,
                        principalTable: "Colis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrackingEvents_Conteneurs_ConteneurId",
                        column: x => x.ConteneurId,
                        principalTable: "Conteneurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TrackingEvents_Vehicules_VehiculeId",
                        column: x => x.VehiculeId,
                        principalTable: "Vehicules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 9, 18, 43, 8, 559, DateTimeKind.Utc).AddTicks(6195));

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ColisId",
                table: "Messages",
                column: "ColisId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_DocumentId",
                table: "Messages",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ExpediteurId",
                table: "Messages",
                column: "ExpediteurId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_VehiculeId",
                table: "Messages",
                column: "VehiculeId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_ColisId",
                table: "TrackingEvents",
                column: "ColisId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_ConteneurId",
                table: "TrackingEvents",
                column: "ConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_EventDate",
                table: "TrackingEvents",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_VehiculeId",
                table: "TrackingEvents",
                column: "VehiculeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "TrackingEvents");

            migrationBuilder.DropColumn(
                name: "RelatedEntityId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RelatedEntityType",
                table: "Notifications");

            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DateCreation",
                value: new DateTime(2025, 12, 7, 15, 38, 58, 997, DateTimeKind.Utc).AddTicks(9725));
        }
    }
}
