using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using TransitManager.Core.Entities;
using TransitManager.Core.Entities.Commerce;
using TransitManager.Core.Enums;

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

        // Commerce
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Quote> Quotes { get; set; } = null!;
        public DbSet<QuoteLine> QuoteLines { get; set; } = null!;
        public DbSet<Barcode> Barcodes { get; set; } = null!;
        public DbSet<Vehicule> Vehicules { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<TrackingEvent> TrackingEvents { get; set; } = null!;
		public DbSet<Notification> Notifications { get; set; } = null!; 
        public DbSet<AppSetting> AppSettings { get; set; } = null!;
        public DbSet<ReceptionControl> ReceptionControls { get; set; } = null!;
        public DbSet<ReceptionIssue> ReceptionIssues { get; set; } = null!;

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

            modelBuilder.Entity<Utilisateur>().HasData(new Utilisateur
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                NomUtilisateur = "admin",
                Nom = "Administrateur",
                Prenom = "Système",
                Email = "admin@transitmanager.com",
                MotDePasseHash = "$2a$11$Tb9CvmOW2h/YNRaP.3QZsOo3jxIN0IN.M4khQYoZu7Ji8i82WyDxu",
                Role = Core.Enums.RoleUtilisateur.Administrateur,
                DateCreation = DateTime.UtcNow,
                Actif = true
            });

            modelBuilder.Entity<Client>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Colis>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Vehicule>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Conteneur>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Paiement>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Document>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<Barcode>().HasQueryFilter(b => b.Colis.Actif);
            modelBuilder.Entity<Message>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<TrackingEvent>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<ReceptionControl>().HasQueryFilter(e => e.Actif);
            modelBuilder.Entity<ReceptionIssue>().HasQueryFilter(e => e.Actif);
        }

        public override int SaveChanges()
        {
            ConvertDatesToUtc();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ConvertDatesToUtc();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ConvertDatesToUtc()
        {
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
                        property.CurrentValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Local).ToUniversalTime();
                    }
                }
            }
        }
    }
}