using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ClientDetailPage : ContentPage
{
    public ClientDetailPage(ClientDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // --- DÉBUT DE L'AJOUT ---
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ClientDetailViewModel vm)
        {
            // La commande s'appelle LoadClientDetailsCommand car elle est générée par le RelayCommand
            await vm.LoadClientDetailsCommand.ExecuteAsync(null);
        }
    }
    // --- FIN DE L'AJOUT ---
}