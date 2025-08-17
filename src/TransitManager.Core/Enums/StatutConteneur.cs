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
		Probleme,
        Ouvert // On le garde pour la logique existante
    }
}