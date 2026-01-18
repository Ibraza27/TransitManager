using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities.Commerce
{
    public class Product
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [MaxLength(20)]
        public string Unit { get; set; } = "h"; // h, pce, m3, km, etc.

        [Column(TypeName = "decimal(5,2)")]
        public decimal VATRate { get; set; } = 20.0m;

        public ProductType Type { get; set; } = ProductType.Goods;

        public bool IsActive { get; set; } = true;
    }
}
