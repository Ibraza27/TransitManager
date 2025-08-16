using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeClient = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TelephonePrincipal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TelephoneSecondaire = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    AdressePrincipale = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdresseLivraison = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ville = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CodePostal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Pays = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, defaultValue: "France"),
                    DateInscription = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Commentaires = table.Column<string>(type: "text", nullable: true),
                    PieceIdentite = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TypePieceIdentite = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NumeroPieceIdentite = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EstClientFidele = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PourcentageRemise = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    BalanceTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    NombreTotalEnvois = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    VolumeTotalExpedié = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conteneurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroDossier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NumeroPlomb = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NomCompagnie = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NomTransitaire = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PaysDestination = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateReception = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateChargement = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateDepart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateArriveeDestination = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateDedouanement = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Commentaires = table.Column<string>(type: "text", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conteneurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomUtilisateur = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MotDePasseHash = table.Column<string>(type: "text", nullable: false),
                    PasswordSalt = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Telephone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhotoProfil = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DerniereConnexion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TentativesConnexionEchouees = table.Column<int>(type: "integer", nullable: false),
                    DateVerrouillage = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DoitChangerMotDePasse = table.Column<bool>(type: "boolean", nullable: false),
                    TokenReinitialisation = table.Column<string>(type: "text", nullable: true),
                    ExpirationToken = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Preferences = table.Column<string>(type: "text", nullable: true),
                    PermissionsSpecifiques = table.Column<string>(type: "text", nullable: true),
                    Theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Langue = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FuseauHoraire = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NotificationsActivees = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationsEmail = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationsSMS = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Colis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConteneurId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateArrivee = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Etat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NombrePieces = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Designation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Poids = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Longueur = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Largeur = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    Hauteur = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ValeurDeclaree = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    EstFragile = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ManipulationSpeciale = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    InstructionsSpeciales = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Photos = table.Column<string>(type: "text", nullable: true),
                    DateDernierScan = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LocalisationActuelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HistoriqueScan = table.Column<string>(type: "jsonb", nullable: true),
                    DateLivraison = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignatureReception = table.Column<string>(type: "text", nullable: true),
                    Commentaires = table.Column<string>(type: "text", nullable: true),
                    Destinataire = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TelephoneDestinataire = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DestinationFinale = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TypeEnvoi = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LivraisonADomicile = table.Column<bool>(type: "boolean", nullable: false),
                    PrixTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SommePayee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NumeroPlomb = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Colis_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Colis_Conteneurs_ConteneurId",
                        column: x => x.ConteneurId,
                        principalTable: "Conteneurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Paiements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroRecu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConteneurId = table.Column<Guid>(type: "uuid", nullable: true),
                    FactureId = table.Column<Guid>(type: "uuid", nullable: true),
                    DatePaiement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    TauxChange = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 1m),
                    ModePaiement = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Banque = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Commentaires = table.Column<string>(type: "text", nullable: true),
                    RecuScanne = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DateEcheance = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RappelEnvoye = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DateDernierRappel = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paiements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paiements_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Paiements_Conteneurs_ConteneurId",
                        column: x => x.ConteneurId,
                        principalTable: "Conteneurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Vehicules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Immatriculation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Marque = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Modele = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Annee = table.Column<int>(type: "integer", nullable: false),
                    Kilometrage = table.Column<int>(type: "integer", nullable: false),
                    DestinationFinale = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ValeurDeclaree = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Destinataire = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TelephoneDestinataire = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Commentaires = table.Column<string>(type: "text", nullable: true),
                    PrixTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SommePayee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EtatDesLieux = table.Column<string>(type: "text", nullable: true),
                    EtatDesLieuxRayures = table.Column<string>(type: "text", nullable: true),
                    Statut = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConteneurId = table.Column<Guid>(type: "uuid", nullable: true),
                    NumeroPlomb = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicules_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehicules_Conteneurs_ConteneurId",
                        column: x => x.ConteneurId,
                        principalTable: "Conteneurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Entite = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntiteId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DateAction = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValeurAvant = table.Column<string>(type: "text", nullable: true),
                    ValeurApres = table.Column<string>(type: "text", nullable: true),
                    AdresseIP = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Commentaires = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priorite = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstLue = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DateLecture = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActionParametre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Barcodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColisId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Barcodes_Colis_ColisId",
                        column: x => x.ColisId,
                        principalTable: "Colis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CheminFichier = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NomFichierOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TailleFichier = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TypeMime = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HashMd5 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    ColisId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConteneurId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaiementId = table.Column<Guid>(type: "uuid", nullable: true),
                    DateExpiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstConfidentiel = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    EstArchive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    DocumentParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    NombreTelechargements = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DateDernierAcces = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DateModification = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreePar = table.Column<string>(type: "text", nullable: true),
                    ModifiePar = table.Column<string>(type: "text", nullable: true),
                    Actif = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Colis_ColisId",
                        column: x => x.ColisId,
                        principalTable: "Colis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Conteneurs_ConteneurId",
                        column: x => x.ConteneurId,
                        principalTable: "Conteneurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Documents_DocumentParentId",
                        column: x => x.DocumentParentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documents_Paiements_PaiementId",
                        column: x => x.PaiementId,
                        principalTable: "Paiements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Utilisateurs",
                columns: new[] { "Id", "Actif", "CreePar", "DateCreation", "DateModification", "DateVerrouillage", "DerniereConnexion", "DoitChangerMotDePasse", "Email", "ExpirationToken", "FuseauHoraire", "Langue", "ModifiePar", "MotDePasseHash", "Nom", "NomUtilisateur", "NotificationsActivees", "NotificationsEmail", "NotificationsSMS", "PasswordSalt", "PermissionsSpecifiques", "PhotoProfil", "Preferences", "Prenom", "Role", "RowVersion", "Telephone", "TentativesConnexionEchouees", "Theme", "TokenReinitialisation" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), true, null, new DateTime(2025, 8, 15, 16, 34, 9, 900, DateTimeKind.Utc).AddTicks(1592), null, null, null, false, "admin@transitmanager.com", null, "Europe/Paris", "fr-FR", null, "$2a$11$fY8.4rEDz1LtUvNFvvJLIO7Snnf1gg3l1u9oP8WnvocBbL.QBgqgq", "Administrateur", "admin", true, true, false, null, null, null, null, "Système", 0, null, null, 0, "Clair", null });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UtilisateurId",
                table: "AuditLogs",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Barcodes_ColisId",
                table: "Barcodes",
                column: "ColisId");

            migrationBuilder.CreateIndex(
                name: "IX_Barcodes_Value",
                table: "Barcodes",
                column: "Value",
                unique: true,
                filter: "\"Actif\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_CodeClient",
                table: "Clients",
                column: "CodeClient",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Email",
                table: "Clients",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_NomPrenom",
                table: "Clients",
                columns: new[] { "Nom", "Prenom" });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TelephonePrincipal",
                table: "Clients",
                column: "TelephonePrincipal");

            migrationBuilder.CreateIndex(
                name: "IX_Colis_ClientId",
                table: "Colis",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Colis_ConteneurId",
                table: "Colis",
                column: "ConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_Colis_DateArrivee",
                table: "Colis",
                column: "DateArrivee");

            migrationBuilder.CreateIndex(
                name: "IX_Colis_NumeroReference",
                table: "Colis",
                column: "NumeroReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colis_Statut",
                table: "Colis",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Conteneurs_NumeroDossier",
                table: "Conteneurs",
                column: "NumeroDossier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conteneurs_Statut",
                table: "Conteneurs",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClientId",
                table: "Documents",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ColisId",
                table: "Documents",
                column: "ColisId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ConteneurId",
                table: "Documents",
                column: "ConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DateCreation",
                table: "Documents",
                column: "DateCreation");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentParentId",
                table: "Documents",
                column: "DocumentParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_EstArchive",
                table: "Documents",
                column: "EstArchive");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PaiementId",
                table: "Documents",
                column: "PaiementId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Type",
                table: "Documents",
                column: "Type");

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
                name: "IX_Notifications_UtilisateurId",
                table: "Notifications",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UtilisateurId_EstLue",
                table: "Notifications",
                columns: new[] { "UtilisateurId", "EstLue" });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ClientId",
                table: "Paiements",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ConteneurId",
                table: "Paiements",
                column: "ConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_DatePaiement",
                table: "Paiements",
                column: "DatePaiement");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_FactureId",
                table: "Paiements",
                column: "FactureId");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ModePaiement",
                table: "Paiements",
                column: "ModePaiement");

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_NumeroRecu",
                table: "Paiements",
                column: "NumeroRecu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_Statut",
                table: "Paiements",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_ClientId",
                table: "Vehicules",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_ConteneurId",
                table: "Vehicules",
                column: "ConteneurId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_Immatriculation",
                table: "Vehicules",
                column: "Immatriculation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicules_Statut",
                table: "Vehicules",
                column: "Statut");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Barcodes");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Vehicules");

            migrationBuilder.DropTable(
                name: "Colis");

            migrationBuilder.DropTable(
                name: "Paiements");

            migrationBuilder.DropTable(
                name: "Utilisateurs");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Conteneurs");
        }
    }
}
