namespace TransitManager.Core.Enums
{
    /// <summary>
    /// Types de notifications
    /// </summary>
    public enum TypeNotification
    {
        /// <summary>Information générale</summary>
        Information,
        
        /// <summary>Avertissement</summary>
        Avertissement,
        
        /// <summary>Erreur</summary>
        Erreur,
        
        /// <summary>Succès d'une opération</summary>
        Succes,
        
        /// <summary>Nouveau colis reçu</summary>
        NouveauColis,
        
        /// <summary>Paiement reçu</summary>
        PaiementRecu,
        
        /// <summary>Conteneur prêt</summary>
        ConteneurPret,
        
        /// <summary>Retard de paiement</summary>
        RetardPaiement,
        
        /// <summary>Document manquant</summary>
        DocumentManquant
    }
}