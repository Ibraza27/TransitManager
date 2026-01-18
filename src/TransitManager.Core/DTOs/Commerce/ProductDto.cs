using System;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs.Commerce
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public string Unit { get; set; }
        public decimal VATRate { get; set; }
        public ProductType Type { get; set; }
    }
}
