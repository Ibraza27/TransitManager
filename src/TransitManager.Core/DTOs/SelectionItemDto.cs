using System;

namespace TransitManager.Core.DTOs
{
    public class SelectionItemDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
    }
}
