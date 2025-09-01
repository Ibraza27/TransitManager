using System;

namespace TransitManager.WPF.Messages
{
    /// <summary>
    /// Message envoyé lorsque le total payé pour une entité (Colis, Véhicule) est mis à jour.
    /// </summary>
    public record EntityTotalPaidUpdatedMessage(Guid EntityId, decimal NewTotalPaid);
}