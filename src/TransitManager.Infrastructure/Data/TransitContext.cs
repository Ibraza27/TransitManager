using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;

namespace TransitManager.Infrastructure.Data
{
    /// <summary>
    /// Contexte Entity Framework pour la base de données Transit Manager
    /// </summary>
    public class TransitContext : DbContext
    {
        private readonly string? _currentUser;

        public TransitContext(DbContextOptions<TransitContext> options) : base(options)
        {
        }

        public TransitContext(DbContextOptions<TransitContext> options, string currentUser) : base(options)
        {
            _currentUser = currentUser;
        }

        // DbSets pour les entités
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Colis> Colis { get; set; } = null!;
        public DbSet<Conteneur> Conteneurs { get; set; } = null!;
        public DbSet<Paiement> Paiements { get; set; } = null!;
        public DbSet<Utilisateur> Utilisateurs { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        /// <summary>
        /// Configuration du modèle
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Appliquer toutes les configurations depuis l'assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransitContext).Assembly);

            // Configuration globale pour les décimales
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // Filtres globaux pour les entités supprimées logiquement


            // Seed data pour l'administrateur par défaut
            modelBuilder.Entity<Utilisateur>().HasData(new Utilisateur
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                NomUtilisateur = "admin",
                Nom = "Administrateur",
                Prenom = "Système",
                Email = "admin@transitmanager.com",
                MotDePasseHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = Core.Enums.RoleUtilisateur.Administrateur,
                DateCreation = DateTime.UtcNow,
                Actif = true
            });
			// AJOUTER CES LIGNES :
			modelBuilder.Entity<Client>().HasQueryFilter(e => e.Actif);
			modelBuilder.Entity<Colis>().HasQueryFilter(e => e.Actif);
			modelBuilder.Entity<Conteneur>().HasQueryFilter(e => e.Actif);
			modelBuilder.Entity<Paiement>().HasQueryFilter(e => e.Actif);
			//modelBuilder.Entity<Utilisateur>().HasQueryFilter(e => e.Actif);
			modelBuilder.Entity<Document>().HasQueryFilter(e => e.Actif);
			// On N'ajoute PAS de filtre pour AuditLog
        }

        /// <summary>
        /// Override SaveChanges pour gérer l'audit automatique
        /// </summary>
        public override int SaveChanges()
        {
            HandleAudit();
            return base.SaveChanges();
        }

        /// <summary>
        /// Override SaveChangesAsync pour gérer l'audit automatique
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            HandleAudit();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Gère l'audit automatique des modifications
        /// </summary>
        private void HandleAudit()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && 
                    (e.State == EntityState.Added || 
                     e.State == EntityState.Modified || 
                     e.State == EntityState.Deleted));

            var now = DateTime.UtcNow;
            var user = _currentUser ?? "System";

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.DateCreation = now;
                        entity.CreePar = user;
                        CreateAuditLog(entry, "CREATE");
                        break;

                    case EntityState.Modified:
                        entity.DateModification = now;
                        entity.ModifiePar = user;
                        CreateAuditLog(entry, "UPDATE");
                        break;

                    case EntityState.Deleted:
                        // Suppression logique au lieu de physique
                        entry.State = EntityState.Modified;
                        entity.Actif = false;
                        entity.DateModification = now;
                        entity.ModifiePar = user;
                        CreateAuditLog(entry, "DELETE");
                        break;
                }
            }
        }

        /// <summary>
        /// Crée un enregistrement d'audit
        /// </summary>
        private void CreateAuditLog(EntityEntry entry, string action)
        {
            var audit = new AuditLog
            {
                Action = action,
                Entite = entry.Entity.GetType().Name,
                EntiteId = entry.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?
                    .CurrentValue?.ToString(),
                DateAction = DateTime.UtcNow,
                AnciennesValeurs = GetValues(entry, false),
                NouvellesValeurs = GetValues(entry, true)
            };

            if (Guid.TryParse(_currentUser, out var userId))
            {
                audit.UtilisateurId = userId;
            }

            AuditLogs.Add(audit);
        }

        /// <summary>
        /// Récupère les valeurs pour l'audit
        /// </summary>
        private static string? GetValues(EntityEntry entry, bool currentValues)
        {
            var values = entry.Properties
                .Where(p => !p.Metadata.IsPrimaryKey())
                .ToDictionary(
                    p => p.Metadata.Name,
                    p => currentValues ? p.CurrentValue : p.OriginalValue
                );

            return System.Text.Json.JsonSerializer.Serialize(values);
        }

        /// <summary>
        /// Configuration de la connexion PostgreSQL
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Database=TransitManager;Username=postgres;Password=postgres")
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
            }
        }
    }
}