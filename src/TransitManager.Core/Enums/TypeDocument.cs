namespace TransitManager.Core.Enums
{
    public enum TypeDocument
    {
        // Administratif Général
        Facture,
        Recu,
        Contrat,
        Devis,
        PieceIdentite,
        JustificatifDomicile,
        Autre,

        // Logistique & Transport
        BordereauExpedition,
        Manifeste,
        ListeColisage,
        DocumentDouanier,
        Etiquette,
        PhotoColis,

        // Véhicules Spécifique
        CarteGrise,
        CertificatCession,
        ActeVente,
        CertificatNonGage,
        Assurance,
        ControleTechnique,
        PhotoConstatVehicule,
        EtatDesLieuxSigne,
        SAV
    }
}