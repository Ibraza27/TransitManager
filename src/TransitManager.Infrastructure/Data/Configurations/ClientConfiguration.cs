using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour l'entité Client
    /// </summary>
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            // Table
            builder.ToTable("Clients");

            // Clé primaire
            builder.HasKey(c => c.Id);

            // Index
            builder.HasIndex(c => c.CodeClient)
                .IsUnique()
                .HasDatabaseName("IX_Clients_CodeClient");

            builder.HasIndex(c => c.TelephonePrincipal)
                .HasDatabaseName("IX_Clients_TelephonePrincipal");

            builder.HasIndex(c => c.Email)
                .HasDatabaseName("IX_Clients_Email");

            builder.HasIndex(c => new { c.Nom, c.Prenom })
                .HasDatabaseName("IX_Clients_NomPrenom");

            // Propriétés
            builder.Property(c => c.CodeClient)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.Nom)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Prenom)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.TelephonePrincipal)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.TelephoneSecondaire)
                .HasMaxLength(20);

            builder.Property(c => c.Email)
                .HasMaxLength(150);

            builder.Property(c => c.AdressePrincipale)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(c => c.AdresseLivraison)
                .HasMaxLength(500);

            builder.Property(c => c.Ville)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(c => c.CodePostal)
                .HasMaxLength(20);

            builder.Property(c => c.Pays)
                .IsRequired(false)
                .HasMaxLength(100)
                .HasDefaultValue("France");

            builder.Property(c => c.Commentaires)
                .HasColumnType("text");

            builder.Property(c => c.PieceIdentite)
                .HasMaxLength(500);

            builder.Property(c => c.TypePieceIdentite)
                .HasMaxLength(50);

            builder.Property(c => c.NumeroPieceIdentite)
                .HasMaxLength(100);

            builder.Property(c => c.PourcentageRemise)
                .HasPrecision(5, 2)
                .HasDefaultValue(0);

            builder.Property(c => c.BalanceTotal)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(c => c.VolumeTotalExpedié)
                .HasPrecision(18, 3)
                .HasDefaultValue(0);

            // Concurrence optimiste
            builder.Property(c => c.RowVersion)
                .IsRowVersion();

            // Relations
            builder.HasMany(c => c.Colis)
                .WithOne(co => co.Client)
                .HasForeignKey(co => co.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Paiements)
                .WithOne(p => p.Client)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Propriétés calculées (ignorées par EF)
            builder.Ignore(c => c.NomComplet);
            builder.Ignore(c => c.AdresseComplete);

            // Valeurs par défaut
            builder.Property(c => c.DateInscription)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(c => c.Actif)
                .HasDefaultValue(true);

            builder.Property(c => c.EstClientFidele)
                .HasDefaultValue(false);

            builder.Property(c => c.NombreTotalEnvois)
                .HasDefaultValue(0);
        }
    }
}