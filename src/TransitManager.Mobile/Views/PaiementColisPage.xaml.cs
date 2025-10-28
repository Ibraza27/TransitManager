using CommunityToolkit.Mvvm.Messaging;
using TransitManager.Core.Messages;
using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class PaiementColisPage : ContentPage
{
    public PaiementColisPage(PaiementColisViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is PaiementColisViewModel vm && Guid.TryParse(vm.ColisId, out Guid id))
        {
            // Envoyer un message pour notifier que le total payé a peut-être changé
            WeakReferenceMessenger.Default.Send(new EntityTotalPaidUpdatedMessage(id, vm.TotalPaye));
        }
    }
}