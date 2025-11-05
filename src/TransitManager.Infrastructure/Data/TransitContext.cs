using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data
{
    public class TransitContext : DbContext
    {
        private readonly string? _currentUser;

        public TransitContext(DbContextOptions<TransitContext> options) : base(options)
        {
            _currentUser = "System";
        }

        // DbSets pour les entités
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Colis> Colis { get; set; } = null!;
        public DbSet<Conteneur> Conteneurs { get; set; } = null!;
        public DbSet<Paiement> Paiements { get; set; } = null!;
        public DbSet<Utilisateur> Utilisateurs { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Barcode> Barcodes { get; set; } = null!;
        public DbSet<Vehicule> Vehicules { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransitContext).Assembly);

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // Hash BCrypt fixe pour le mot de passe "Admin@123"
            // Ce hash est statique pour garantir la cohérence entre les migrations
            // Hash: $2a$11$47CimAPLqf80X5ildRmPXuC0TWgjvHAIA7CeifbveROmjA1zR0dOu
            modelBuilder.Entity<Utilisateur>().HasData(new Utilisateur
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                NomUtilisateur = "admin",
                Nom = "Administrateur",
                Prenom = "Système",
                Email = "admin@transitmanager.com",
                MotDePasseHash = "$2a$11$47CimAPLqf80X5ildRmPXuC0TWgjvHAIA7CeifbveROmjA1zR0dOu",
                Role = Core.Enums.RoleUtilisateur.Administrateur,
                DateCreation = new DateTime(2025, 8, 27, 21, 36, 38, 895, DateTimeKind.Utc).AddTicks(8319),
                Actif = true
            });

            modelBuilder.Entity<Client>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Colis>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Vehicule>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Conteneur>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Paiement>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Document>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Barcode>().HasQueryFilter(b => b.Colis.Actif);
        }

        public override int SaveChanges()
        {
            HandleAuditAndDates(); // <-- Appel de la méthode combinée
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            HandleAuditAndDates(); // <-- Appel de la méthode combinée
            return await base.SaveChangesAsync(cancellationToken);
        }

        // --- NOUVELLE MÉTHODE POUR CONVERTIR TOUTES LES DATES EN UTC ---
        private void ConvertDatesToUtc()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                // On recherche toutes les propriétés de type DateTime ou DateTime?
                var properties = entry.CurrentValues.Properties
                    .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?));

                foreach (var property in properties)
                {
                    // Si la valeur est une DateTime et qu'elle n'est pas déjà en UTC
                    if (entry.CurrentValues[property] is DateTime dateTimeValue && dateTimeValue.Kind != DateTimeKind.Utc)
                    {
                        // On la convertit en UTC avant de la sauvegarder
                        entry.CurrentValues[property] = dateTimeValue.ToUniversalTime();
                    }
                }
            }
        }

        // --- L'ANCIENNE MÉTHODE HandleAudit A ÉTÉ RENOMMÉE ET MODIFIÉE ---
        private void HandleAuditAndDates()
        {
            // ======================= DÉBUT DE LA CORRECTION =======================
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var dateProperties = entry.Properties
                    .Where(p => p.Metadata.ClrType == typeof(DateTime) || p.Metadata.ClrType == typeof(DateTime?));

                foreach (var property in dateProperties)
                {
                    if (property.CurrentValue is DateTime dateTimeValue && dateTimeValue.Kind != DateTimeKind.Utc)
                    {
                        // Logique améliorée et plus robuste :
                        // 1. Si la date est 'Unspecified' (venant d'un DatePicker), on la traite comme une date locale.
                        // 2. Ensuite, on la convertit en UTC.
                        // Cela couvre à la fois les cas 'Local' et 'Unspecified'.
                        property.CurrentValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Local).ToUniversalTime();
                    }
                }
            }
            // ======================== FIN DE LA CORRECTION ========================

            var auditEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                            e.Entity.GetType() != typeof(AuditLog) &&
                            (e.State == EntityState.Added ||
                             e.State == EntityState.Modified ||
                             e.State == EntityState.Deleted))
                .ToList();

            var now = DateTime.UtcNow;
            var user = _currentUser ?? "System";

            foreach (var entry in auditEntries)
            {
                var entity = (BaseEntity)entry.Entity;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.DateCreation = now;
                        entity.CreePar = user;
                        CreateAuditLog(entry, "CREATE");
                        break;
                    // ... (le reste de la méthode est identique)
                    case EntityState.Modified:
                        entity.DateModification = now;
                        entity.ModifiePar = user;
                        CreateAuditLog(entry, "UPDATE");
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entity.Actif = false;
                        entity.DateModification = now;
                        entity.ModifiePar = user;
                        CreateAuditLog(entry, "DELETE");
                        break;
                }
            }
        }

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
                ValeurAvant = GetValues(entry, false),
                ValeurApres = GetValues(entry, true)
            };

            if (Guid.TryParse(_currentUser, out var userId) && userId != Guid.Empty)
            {
                audit.UtilisateurId = userId;
            }
            else
            {
                audit.UtilisateurId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            }

            AuditLogs.Add(audit);
        }

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