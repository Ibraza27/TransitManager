using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;

namespace TransitManager.Infrastructure.Data.Configurations
{
    public class ColisConfiguration : IEntityTypeConfiguration<Colis>
    {
        public void Configure(EntityTypeBuilder<Colis> builder)
        {
            builder.ToTable("Colis");
            builder.HasKey(c => c.Id);

            builder.HasIndex(c => c.NumeroReference).IsUnique();
            builder.HasIndex(c => c.ClientId);
            builder.HasIndex(c => c.ConteneurId);
            builder.HasIndex(c => c.Statut);
            builder.HasIndex(c => c.DateArrivee);

            builder.Property(c => c.NumeroReference).IsRequired().HasMaxLength(50);
            builder.Property(c => c.Designation).IsRequired().HasMaxLength(500);
            builder.Property(c => c.NumeroPlomb).HasMaxLength(50); // NOUVELLE LIGNE

            builder.Property(c => c.Poids).HasPrecision(10, 3).HasDefaultValue(0);
            builder.Property(c => c.Longueur).HasPrecision(10, 2).HasDefaultValue(0);
            builder.Property(c => c.Largeur).HasPrecision(10, 2).HasDefaultValue(0);
            builder.Property(c => c.Hauteur).HasPrecision(10, 2).HasDefaultValue(0);
            builder.Property(c => c.ValeurDeclaree).HasPrecision(18, 2).HasDefaultValue(0);
            builder.Property(c => c.InstructionsSpeciales).HasMaxLength(1000);
            builder.Property(c => c.Photos).HasColumnType("text");
            builder.Property(c => c.LocalisationActuelle).HasMaxLength(200);
            builder.Property(c => c.HistoriqueScan).HasColumnType("jsonb");
            builder.Property(c => c.Destinataire).HasMaxLength(200);
            builder.Property(c => c.SignatureReception).HasColumnType("text");
            builder.Property(c => c.Commentaires).HasColumnType("text");

            builder.Property(c => c.Statut).HasConversion<string>().HasMaxLength(50);
            builder.Property(c => c.Etat).HasConversion<string>().HasMaxLength(50);
            builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(c => c.TypeEnvoi).HasConversion<string>().HasMaxLength(50);

            builder.HasOne(c => c.Client)
                .WithMany(cl => cl.Colis)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Conteneur)
                .WithMany(co => co.Colis)
                .HasForeignKey(c => c.ConteneurId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Ignore(c => c.Volume);
            builder.Ignore(c => c.PoidsVolumetrique);
            builder.Ignore(c => c.PoidsFacturable);

            builder.Property(c => c.DateArrivee).HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(c => c.Actif).HasDefaultValue(true);
            builder.Property(c => c.EstFragile).HasDefaultValue(false);
            builder.Property(c => c.ManipulationSpeciale).HasDefaultValue(false);
            builder.Property(c => c.NombrePieces).HasDefaultValue(1);
            builder.Property(c => c.RowVersion).IsRowVersion();
        }
    }
}