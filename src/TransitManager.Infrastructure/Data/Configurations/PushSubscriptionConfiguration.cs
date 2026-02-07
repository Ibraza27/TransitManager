using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
    {
        public void Configure(EntityTypeBuilder<PushSubscription> builder)
        {
            builder.ToTable("PushSubscriptions");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Endpoint)
                .IsRequired()
                .HasMaxLength(2048);

            builder.Property(p => p.P256dh)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(p => p.Auth)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(p => p.UserAgent)
                .HasMaxLength(500);

            builder.Property(p => p.DateCreation)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Index unique : un seul abonnement par endpoint
            builder.HasIndex(p => p.Endpoint).IsUnique();

            // Index pour recherche par utilisateur
            builder.HasIndex(p => p.UtilisateurId);

            // Relation
            builder.HasOne(p => p.Utilisateur)
                .WithMany()
                .HasForeignKey(p => p.UtilisateurId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
