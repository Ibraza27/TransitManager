using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    public class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
    {
        public void Configure(EntityTypeBuilder<Utilisateur> builder)
        {
            builder.ToTable("Utilisateurs");
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.NomUtilisateur)
                .IsRequired()
                .HasMaxLength(50);
            
            // ... autres configurations ...
        }
    }
}