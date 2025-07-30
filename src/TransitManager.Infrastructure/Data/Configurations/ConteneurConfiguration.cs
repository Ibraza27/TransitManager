using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour l'entité Conteneur
    /// </summary>
    public class ConteneurConfiguration : IEntityTypeConfiguration<Conteneur>
    {
        public void Configure(EntityTypeBuilder<Conteneur> builder)
        {
            // Table
            builder.ToTable("Conteneurs");

            // Clé primaire
            builder.HasKey(c => c.Id);

            // Index
            builder.HasIndex(c => c.NumeroDossier)
                .IsUnique()
                .HasDatabaseName("IX_Conteneurs_NumeroDossier");

            builder.HasIndex(c => c.Destination)
                .HasDatabaseName("IX_Conteneurs_Destination");

            builder.HasIndex(c => c.PaysDestination)
                .HasDatabaseName("IX_Conteneurs_PaysDestination");

            builder.HasIndex(c => c.Statut)
                .HasDatabaseName("IX_Conteneurs_Statut");

            builder.HasIndex(c => c.DateDepartPrevue)
                .HasDatabaseName("IX_Conteneurs_DateDepartPrevue");

            builder.HasIndex(c => c.DateOuverture)
                .HasDatabaseName("IX_Conteneurs_DateOuverture");

            // Propriétés
            builder.Property(c => c.NumeroDossier)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(c => c.Destination)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.PaysDestination)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.CapaciteVolume)
                .HasPrecision(10, 2)
                .HasDefaultValue(33); // 33 m³ par défaut

            builder.Property(c => c.CapacitePoids)
                .HasPrecision(10, 2)
                .HasDefaultValue(28000); // 28 tonnes par défaut

            builder.Property(c => c.Transporteur)
                .HasMaxLength(200);

            builder.Property(c => c.NumeroTracking)
                .HasMaxLength(100);

            builder.Property(c => c.NumeroNavireVol)
                .HasMaxLength(100);

            builder.Property(c => c.DocumentsDouaniers)
                .HasColumnType("text");

            builder.Property(c => c.ManifesteExpedition)
                .HasMaxLength(500);

            builder.Property(c => c.ListeColisage)
                .HasMaxLength(500);

            builder.Property(c => c.FraisTransport)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(c => c.FraisDedouanement)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(c => c.AutresFrais)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(c => c.Commentaires)
                .HasColumnType("text");

            // Enums
            builder.Property(c => c.TypeEnvoi)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(c => c.Statut)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Relations
            builder.HasMany(c => c.Colis)
                .WithOne(co => co.Conteneur)
                .HasForeignKey(co => co.ConteneurId)
                .OnDelete(DeleteBehavior.SetNull);

            // Propriétés calculées (ignorées par EF)
            builder.Ignore(c => c.VolumeUtilise);
            builder.Ignore(c => c.PoidsUtilise);
            builder.Ignore(c => c.TauxRemplissageVolume);
            builder.Ignore(c => c.TauxRemplissagePoids);
            builder.Ignore(c => c.NombreClients);
            builder.Ignore(c => c.NombreColis);
            builder.Ignore(c => c.CoutTotal);
            builder.Ignore(c => c.PeutRecevoirColis);

            // Valeurs par défaut
            builder.Property(c => c.DateOuverture)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(c => c.Actif)
                .HasDefaultValue(true);

            // Concurrence optimiste
            builder.Property(c => c.RowVersion)
                .IsRowVersion();
        }
    }
}