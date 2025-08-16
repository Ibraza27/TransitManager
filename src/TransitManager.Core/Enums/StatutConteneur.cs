namespace TransitManager.Core.Enums
{
    public enum StatutConteneur
    {
        Reçu,
        EnPreparation,
        EnTransit,
        Arrive,
        EnDedouanement,
        Livre,
        Cloture,
        Annule,
        Ouvert // On le garde pour la logique existante
    }
}