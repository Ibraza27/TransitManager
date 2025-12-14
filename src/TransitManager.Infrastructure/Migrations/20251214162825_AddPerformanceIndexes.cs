using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// Index pour les filtres "Actif" (très fréquent)
			migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Colis_Actif\" ON \"Colis\" (\"Actif\") WHERE \"Actif\" = true;");
			migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Vehicules_Actif\" ON \"Vehicules\" (\"Actif\") WHERE \"Actif\" = true;");
			migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Conteneurs_Actif\" ON \"Conteneurs\" (\"Actif\") WHERE \"Actif\" = true;");

			// Index composites pour les recherches fréquentes (Client, Conteneur, Statut)
			migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Colis_ClientId_Actif_DateArrivee\" ON \"Colis\" (\"ClientId\", \"Actif\", \"DateArrivee\" DESC);");
			migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Colis_ConteneurId_Actif_Statut\" ON \"Colis\" (\"ConteneurId\", \"Actif\", \"Statut\");");
			
			// Index pour les notifications (Critique pour le compteur temps réel)
			migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Notifications_UtilisateurId_EstLue_DateCreation\" ON \"Notifications\" (\"UtilisateurId\", \"EstLue\", \"DateCreation\" DESC);");
			
			// Index pour l'historique et l'audit
			migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_TrackingEvents_ColisId_EventDate\" ON \"TrackingEvents\" (\"ColisId\", \"EventDate\" DESC);");
			migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_AuditLogs_EntiteId_DateAction\" ON \"AuditLogs\" (\"EntiteId\", \"DateAction\" DESC);");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Colis_Actif\";");
			migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Vehicules_Actif\";");
			migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Conteneurs_Actif\";");
			migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Colis_ClientId_Actif_DateArrivee\";");
			migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Colis_ConteneurId_Actif_Statut\";");
			migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Notifications_UtilisateurId_EstLue_DateCreation\";");
			migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_TrackingEvents_ColisId_EventDate\";");
			migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_AuditLogs_EntiteId_DateAction\";");
		}
    }
}
