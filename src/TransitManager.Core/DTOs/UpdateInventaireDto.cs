namespace TransitManager.Core.DTOs
{
	public class UpdateInventaireDto
	{
		public Guid ColisId { get; set; }
		public string? InventaireJson { get; set; }
		public int TotalPieces { get; set; }
		public decimal TotalValeurDeclaree { get; set; }
	}
}