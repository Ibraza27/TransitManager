using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class UpdateDocumentDto
    {
        public string Nom { get; set; } = string.Empty;
        public TypeDocument Type { get; set; }
    }
}
