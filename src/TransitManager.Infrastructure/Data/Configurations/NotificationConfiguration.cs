using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configuration Entity Framework pour l'entité Notification
    /// </summary>
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            // Table
            builder.ToTable("Notifications");

            // Clé primaire
            builder.HasKey(n => n.Id);

            // Index
            builder.HasIndex(n => n.UtilisateurId)
                .HasDatabaseName("IX_Notifications_UtilisateurId");

            builder.HasIndex(n => n.Type)
                .HasDatabaseName("IX_Notifications_Type");

            builder.HasIndex(n => n.EstLue)
                .HasDatabaseName("IX_Notifications_EstLue");

            builder.HasIndex(n => n.DateCreation)
                .HasDatabaseName("IX_Notifications_DateCreation");

            builder.HasIndex(n => new { n.UtilisateurId, n.EstLue })
                .HasDatabaseName("IX_Notifications_UtilisateurId_EstLue");

            // Propriétés
            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Message)
                .IsRequired()
                .HasColumnType("text");

            builder.Property(n => n.ActionUrl)
                .HasMaxLength(500);

            builder.Property(n => n.ActionParametre)
                .HasMaxLength(100);


            // Enums
            builder.Property(n => n.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(n => n.Priorite)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Relations
            builder.HasOne(n => n.Utilisateur)
                .WithMany()
                .HasForeignKey(n => n.UtilisateurId)
                .OnDelete(DeleteBehavior.Cascade);

            // Valeurs par défaut
            builder.Property(n => n.EstLue)
                .HasDefaultValue(false);

            builder.Property(n => n.DateCreation)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(n => n.Actif)
                .HasDefaultValue(true);

            // Concurrence optimiste
            builder.Property(n => n.RowVersion)
                .IsRowVersion();
        }
    }
}