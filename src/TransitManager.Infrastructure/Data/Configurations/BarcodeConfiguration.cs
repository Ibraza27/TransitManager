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

			// Un code-barres ACTIF doit être unique.
			// Les codes-barres inactifs (supprimés) sont ignorés par la contrainte.
			builder.HasIndex(b => b.Value)
				.IsUnique()
				.HasFilter("\"Actif\" = true");

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