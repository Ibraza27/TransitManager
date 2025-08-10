// Fichier: src/TransitManager.Core/Entities/Barcode.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace TransitManager.Core.Entities
{
    public class Barcode : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Value { get; set; } = string.Empty;

        public Guid ColisId { get; set; }
        public virtual Colis Colis { get; set; } = null!;
    }
}