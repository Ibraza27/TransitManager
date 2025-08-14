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

            builder.Property(v => v.Immatriculation).IsRequired().HasMaxLength(50);
            builder.Property(v => v.Marque).IsRequired().HasMaxLength(100);
            builder.Property(v => v.Modele).IsRequired().HasMaxLength(100);
            builder.Property(v => v.DestinationFinale).IsRequired().HasMaxLength(200);
            
            builder.Property(v => v.PrixTotal).HasPrecision(18, 2);
            builder.Property(v => v.SommePayee).HasPrecision(18, 2);
            builder.Property(v => v.ValeurDeclaree).HasPrecision(18, 2);

            builder.Property(v => v.Type).HasConversion<string>().HasMaxLength(50);

            // Relation avec Client
            builder.HasOne(v => v.Client)
                .WithMany() // Un client peut avoir plusieurs véhicules
                .HasForeignKey(v => v.ClientId)
                .OnDelete(DeleteBehavior.Restrict); // Ne pas supprimer le client si un véhicule lui est associé

            builder.Ignore(v => v.RestantAPayer);
        }
    }
}