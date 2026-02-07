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

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Endpoint)
                .IsRequired();

            builder.HasIndex(e => e.Endpoint)
                .IsUnique();

            builder.HasOne(e => e.Utilisateur)
                .WithMany()
                .HasForeignKey(e => e.UtilisateurId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
