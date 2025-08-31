using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour l'entité Paiement
    /// </summary>
    public class PaiementConfiguration : IEntityTypeConfiguration<Paiement>
    {
        public void Configure(EntityTypeBuilder<Paiement> builder)
        {
            // Table
            builder.ToTable("Paiements");

            // Clé primaire
            builder.HasKey(p => p.Id);

            // Index
            builder.HasIndex(p => p.NumeroRecu)
                .IsUnique()
                .HasDatabaseName("IX_Paiements_NumeroRecu");

            builder.HasIndex(p => p.ClientId)
                .HasDatabaseName("IX_Paiements_ClientId");

            builder.HasIndex(p => p.ConteneurId)
                .HasDatabaseName("IX_Paiements_ConteneurId");

            builder.HasIndex(p => p.FactureId)
                .HasDatabaseName("IX_Paiements_FactureId");

            builder.HasIndex(p => p.DatePaiement)
                .HasDatabaseName("IX_Paiements_DatePaiement");

            builder.HasIndex(p => p.Statut)
                .HasDatabaseName("IX_Paiements_Statut");

            builder.HasIndex(p => p.ModePaiement)
                .HasDatabaseName("IX_Paiements_ModePaiement");

            // Propriétés
            builder.Property(p => p.NumeroRecu)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Montant)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(p => p.Devise)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("EUR");

            builder.Property(p => p.TauxChange)
                .HasPrecision(10, 6)
                .HasDefaultValue(1);

            builder.Property(p => p.Reference)
                .HasMaxLength(100);

            builder.Property(p => p.Banque)
                .HasMaxLength(100);

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.Property(p => p.Commentaires)
                .HasColumnType("text");

            builder.Property(p => p.RecuScanne)
                .HasMaxLength(500);

            // Enums
            builder.Property(p => p.ModePaiement)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(p => p.Statut)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Relations
            builder.HasOne(p => p.Client)
                .WithMany(c => c.Paiements)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Conteneur)
                .WithMany()
                .HasForeignKey(p => p.ConteneurId)
                .OnDelete(DeleteBehavior.SetNull);
				
			builder.HasOne(p => p.Colis)
				.WithMany(c => c.Paiements)
				.HasForeignKey(p => p.ColisId)
				.OnDelete(DeleteBehavior.Cascade); // Supprime les paiements si le colis est supprimé
	

            // Propriétés calculées (ignorées par EF)
            builder.Ignore(p => p.MontantLocal);
            builder.Ignore(p => p.EstEnRetard);

            // Valeurs par défaut
            builder.Property(p => p.DatePaiement)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(p => p.Actif)
                .HasDefaultValue(true);

            builder.Property(p => p.RappelEnvoye)
                .HasDefaultValue(false);

            // Concurrence optimiste
            builder.Property(p => p.RowVersion)
                .IsRowVersion();
        }
    }
}