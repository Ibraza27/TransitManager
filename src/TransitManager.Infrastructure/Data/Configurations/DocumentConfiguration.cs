using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour l'entité Document
    /// </summary>
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            // Table
            builder.ToTable("Documents");

            // Clé primaire
            builder.HasKey(d => d.Id);

            // Index
            builder.HasIndex(d => d.ClientId)
                .HasDatabaseName("IX_Documents_ClientId");

            builder.HasIndex(d => d.ColisId)
                .HasDatabaseName("IX_Documents_ColisId");

            builder.HasIndex(d => d.ConteneurId)
                .HasDatabaseName("IX_Documents_ConteneurId");

            builder.HasIndex(d => d.PaiementId)
                .HasDatabaseName("IX_Documents_PaiementId");

            builder.HasIndex(d => d.Type)
                .HasDatabaseName("IX_Documents_Type");

            builder.HasIndex(d => d.DateCreation)
                .HasDatabaseName("IX_Documents_DateCreation");

            builder.HasIndex(d => d.EstArchive)
                .HasDatabaseName("IX_Documents_EstArchive");

            // Propriétés
            builder.Property(d => d.Nom)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.Description)
                .HasMaxLength(500);

            builder.Property(d => d.CheminFichier)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(d => d.NomFichierOriginal)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.Extension)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(d => d.TypeMime)
                .HasMaxLength(100);

            builder.Property(d => d.HashMd5)
                .HasMaxLength(32);

            builder.Property(d => d.Tags)
                .HasMaxLength(500);

            builder.Property(d => d.TailleFichier)
                .HasDefaultValue(0);

            builder.Property(d => d.Version)
                .HasDefaultValue(1);

            builder.Property(d => d.NombreTelechargements)
                .HasDefaultValue(0);

            // Enums
            builder.Property(d => d.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Relations
            builder.HasOne(d => d.Client)
                .WithMany()
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(d => d.Colis)
                .WithMany()
                .HasForeignKey(d => d.ColisId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(d => d.Conteneur)
                .WithMany()
                .HasForeignKey(d => d.ConteneurId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(d => d.Paiement)
                .WithMany()
                .HasForeignKey(d => d.PaiementId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(d => d.DocumentParent)
                .WithMany()
                .HasForeignKey(d => d.DocumentParentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Propriétés calculées (ignorées par EF)
            builder.Ignore(d => d.NomComplet);
            builder.Ignore(d => d.TailleFormatee);
            builder.Ignore(d => d.EstExpire);

            // Valeurs par défaut
            builder.Property(d => d.DateCreation)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(d => d.Actif)
                .HasDefaultValue(true);

            builder.Property(d => d.EstConfidentiel)
                .HasDefaultValue(false);

            builder.Property(d => d.EstArchive)
                .HasDefaultValue(false);

            // Concurrence optimiste
            builder.Property(d => d.RowVersion)
                .IsRowVersion();
        }
    }
}