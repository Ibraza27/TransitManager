using CommunityToolkit.Mvvm.Messaging; // <-- AJOUTER CE USING
using TransitManager.Mobile.ViewModels;
using TransitManager.Core.Messages; // <-- AJOUTER CE USING

namespace TransitManager.Mobile.Views;

public partial class PaiementVehiculePage : ContentPage
{
    public PaiementVehiculePage(PaiementVehiculeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // --- AJOUTER CETTE MÉTHODE ---
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is PaiementVehiculeViewModel vm && Guid.TryParse(vm.VehiculeId, out Guid id))
        {
            // Envoyer un message à toute l'application pour dire que ce véhicule a été mis à jour
            WeakReferenceMessenger.Default.Send(new EntityTotalPaidUpdatedMessage(id, vm.TotalPaye));
        }
    }
}