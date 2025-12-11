using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Contenu).IsRequired();

            // Relation Expéditeur (Si on supprime un user, on garde ses messages pour l'historique ou on supprime ?)
            // Ici : Restrict pour éviter de supprimer l'historique par erreur.
            builder.HasOne(m => m.Expediteur)
                .WithMany()
                .HasForeignKey(m => m.ExpediteurId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relation Colis (Cascade : si on supprime le colis, on supprime la discussion)
            builder.HasOne(m => m.Colis)
                .WithMany() // Ajouter ICollection<Message> dans Colis.cs plus tard si besoin
                .HasForeignKey(m => m.ColisId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relation Vehicule
            builder.HasOne(m => m.Vehicule)
                .WithMany()
                .HasForeignKey(m => m.VehiculeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relation Document (Si on supprime le message, on ne supprime PAS le document car il peut être ailleurs)
            builder.HasOne(m => m.PieceJointe)
                .WithMany()
                .HasForeignKey(m => m.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}