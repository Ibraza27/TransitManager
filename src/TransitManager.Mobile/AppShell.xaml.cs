using TransitManager.Mobile.Views; // N'oubliez pas ce using

namespace TransitManager.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // --- AJOUTER CETTE LIGNE ---
            Routing.RegisterRoute(nameof(ClientDetailPage), typeof(ClientDetailPage));
			Routing.RegisterRoute(nameof(AddEditClientPage), typeof(AddEditClientPage));
			Routing.RegisterRoute(nameof(ColisDetailPage), typeof(ColisDetailPage));
			Routing.RegisterRoute(nameof(AddEditColisPage), typeof(AddEditColisPage));
			Routing.RegisterRoute(nameof(VehiculeDetailPage), typeof(VehiculeDetailPage));
			Routing.RegisterRoute(nameof(AddEditVehiculePage), typeof(AddEditVehiculePage));
			Routing.RegisterRoute(nameof(EtatDesLieuxPage), typeof(EtatDesLieuxPage));
			Routing.RegisterRoute(nameof(EditEtatDesLieuxPage), typeof(EditEtatDesLieuxPage));
			Routing.RegisterRoute(nameof(PaiementVehiculePage), typeof(PaiementVehiculePage));
			Routing.RegisterRoute(nameof(PaiementColisPage), typeof(PaiementColisPage));
			Routing.RegisterRoute(nameof(AddEditPaiementPage), typeof(AddEditPaiementPage));
			Routing.RegisterRoute(nameof(InventairePage), typeof(InventairePage));
			
        }
    }
}