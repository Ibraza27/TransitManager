using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            // Table
            builder.ToTable("Notifications");

            // Clé primaire
            builder.HasKey(n => n.Id);

            // Propriétés requises
            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Message)
                .IsRequired();

            // Propriétés visuelles et de navigation (Optionnelles)
            builder.Property(n => n.Icone).HasMaxLength(50);
            builder.Property(n => n.Couleur).HasMaxLength(50);
            builder.Property(n => n.ActionUrl).HasMaxLength(500);
            
            // --- CORRECTION : On mappe les nouvelles propriétés ---
            builder.Property(n => n.RelatedEntityType).HasMaxLength(50);
            // On NE mappe PLUS ActionParametre car elle n'existe plus

            // Enums (Conversion en string pour lisibilité en BDD)
            builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(n => n.Priorite).HasConversion<string>().HasMaxLength(50);
            builder.Property(n => n.Categorie).HasConversion<string>().HasMaxLength(50);

            // Relations
            builder.HasOne(n => n.Utilisateur)
                .WithMany()
                .HasForeignKey(n => n.UtilisateurId)
                .OnDelete(DeleteBehavior.Cascade);

            // Valeurs par défaut
            builder.Property(n => n.EstLue).HasDefaultValue(false);
            builder.Property(n => n.DateCreation).HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(n => n.Actif).HasDefaultValue(true);
        }
    }
}