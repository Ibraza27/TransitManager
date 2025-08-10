using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransitManager.Core.Entities;

namespace TransitManager.Infrastructure.Data.Configurations
{
    public class BarcodeConfiguration : IEntityTypeConfiguration<Barcode>
    {
        public void Configure(EntityTypeBuilder<Barcode> builder)
        {
            builder.ToTable("Barcodes");
            builder.HasKey(b => b.Id);

            // Un code-barres doit être unique dans tout le système
            builder.HasIndex(b => b.Value).IsUnique();

            builder.Property(b => b.Value)
                .IsRequired()
                .HasMaxLength(100);

            // Relation avec Colis : un code-barres appartient à un seul colis
            builder.HasOne(b => b.Colis)
                .WithMany(c => c.Barcodes) // La collection que nous avons ajoutée à Colis
                .HasForeignKey(b => b.ColisId);
        }
    }
}