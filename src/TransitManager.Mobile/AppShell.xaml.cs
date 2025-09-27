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
        }
    }
}