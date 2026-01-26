namespace TransitManager.Core.Enums
{
    public enum InvoiceStatus
    {
        Draft,      // Brouillon
        Sent,       // Envoyée
        Viewed,     // Vue
        Paid,       // Payée
        Overdue,    // En retard
        Cancelled,  // Annulée
        Unpaid      // Impayée (used for logic, usually same as Sent but past due logic is separate)
    }
}
