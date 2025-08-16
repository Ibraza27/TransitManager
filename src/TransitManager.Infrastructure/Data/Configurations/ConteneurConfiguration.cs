using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    public class ConteneurConfiguration : IEntityTypeConfiguration<Conteneur>
    {
        public void Configure(EntityTypeBuilder<Conteneur> builder)
        {
            builder.ToTable("Conteneurs");
            builder.HasKey(c => c.Id);

            builder.HasIndex(c => c.NumeroDossier).IsUnique();
            builder.HasIndex(c => c.Statut);

            builder.Property(c => c.NumeroDossier).IsRequired().HasMaxLength(50);
            builder.Property(c => c.NumeroPlomb).HasMaxLength(50);
            builder.Property(c => c.NomCompagnie).HasMaxLength(200);
            builder.Property(c => c.NomTransitaire).HasMaxLength(200);
            builder.Property(c => c.Destination).IsRequired().HasMaxLength(200);
            builder.Property(c => c.PaysDestination).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Commentaires).HasColumnType("text");
            
            builder.Property(c => c.Statut).HasConversion<string>().HasMaxLength(50);

            // Relations
            builder.HasMany(c => c.Colis)
                .WithOne(co => co.Conteneur)
                .HasForeignKey(co => co.ConteneurId)
                .OnDelete(DeleteBehavior.SetNull);
                
            builder.HasMany(c => c.Vehicules)
                .WithOne(v => v.Conteneur)
                .HasForeignKey(v => v.ConteneurId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignorer les propriétés calculées
            builder.Ignore(c => c.NombreColis);
            builder.Ignore(c => c.NombreVehicules);
            builder.Ignore(c => c.ClientsDistincts);
            
            builder.Property(c => c.Actif).HasDefaultValue(true);
            builder.Property(c => c.RowVersion).IsRowVersion();
        }
    }
}