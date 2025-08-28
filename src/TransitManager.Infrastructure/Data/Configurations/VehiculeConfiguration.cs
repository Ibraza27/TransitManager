using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    public class VehiculeConfiguration : IEntityTypeConfiguration<Vehicule>
    {
        public void Configure(EntityTypeBuilder<Vehicule> builder)
        {
            builder.ToTable("Vehicules");
            builder.HasKey(v => v.Id);

            builder.HasIndex(v => v.Immatriculation).IsUnique();
            builder.HasIndex(v => v.ConteneurId); 
            builder.HasIndex(v => v.Statut); 

            builder.Property(v => v.Immatriculation).IsRequired().HasMaxLength(50);
            builder.Property(v => v.Marque).IsRequired().HasMaxLength(100);
            builder.Property(v => v.Modele).IsRequired().HasMaxLength(100);
            builder.Property(v => v.DestinationFinale).IsRequired().HasMaxLength(200);
            
            builder.Property(v => v.PrixTotal).HasPrecision(18, 2);
            builder.Property(v => v.SommePayee).HasPrecision(18, 2);
            builder.Property(v => v.ValeurDeclaree).HasPrecision(18, 2);
            builder.Property(v => v.NumeroPlomb).HasMaxLength(50);

            builder.Property(v => v.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(v => v.Statut).HasConversion<string>().HasMaxLength(50);

            // La relation avec Conteneur (déjà définie dans ConteneurConfiguration, mais la clé étrangère est ici)
            builder.HasOne(v => v.Conteneur)
                .WithMany(c => c.Vehicules)
                .HasForeignKey(v => v.ConteneurId);

            builder.Ignore(v => v.RestantAPayer);
        }
    }
}