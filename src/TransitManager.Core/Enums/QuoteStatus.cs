namespace TransitManager.Core.Enums
{
    public enum QuoteStatus
    {
        Draft,      // Brouillon
        Sent,       // Envoyé
        Viewed,     // Vu par le client
        Accepted,   // Accepté
        Rejected,   // Refusé
        ChangeRequested, // Modification demandée
        Converted,  // Converti en facture
        Deleted     // Supprimé (Corbeille)
    }
}
