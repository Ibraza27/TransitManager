namespace TransitManager.Core.Enums
{
    public enum CategorieNotification
    {
        Systeme,           // Info générale
        StatutColis,       // 📦 Changement d'état
        StatutVehicule,    // 🚗 Changement d'état
        StatutConteneur,   // 🚢 Arrivée, Départ
        Paiement,          // 💰 Nouveau paiement, Validation
        Document,          // 📄 Ajout de doc
        Message,           // 💬 Ancien (générique)
        Inventaire,        // 📋 Modif inventaire
        NouveauClient,     // 👤 Admin only
        
        // --- CEUX QUI MANQUAIENT ---
        AlerteDouane,      // 🛃 Douane
        NouveauMessage,    // 💬 Chat
        Commerce           // 💰 Devis / Facture (Accepté, Refusé, etc.)
    }
}