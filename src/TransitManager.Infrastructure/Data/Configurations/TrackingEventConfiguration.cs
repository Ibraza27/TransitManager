using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    public class TrackingEventConfiguration : IEntityTypeConfiguration<TrackingEvent>
    {
        public void Configure(EntityTypeBuilder<TrackingEvent> builder)
        {
            builder.ToTable("TrackingEvents");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Description).IsRequired().HasMaxLength(200);
            
            // Index pour la performance (on trie souvent par date pour un item donné)
            builder.HasIndex(t => t.ColisId);
            builder.HasIndex(t => t.VehiculeId);
            builder.HasIndex(t => t.EventDate);

            // Relations
            builder.HasOne(t => t.Colis)
                .WithMany()
                .HasForeignKey(t => t.ColisId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Vehicule)
                .WithMany()
                .HasForeignKey(t => t.VehiculeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.Conteneur)
                .WithMany()
                .HasForeignKey(t => t.ConteneurId)
                .OnDelete(DeleteBehavior.SetNull); // Si on supprime un conteneur, on garde l'historique du colis
        }
    }
}