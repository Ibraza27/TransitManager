using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class VehiculeDetailPage : ContentPage
{
    public VehiculeDetailPage(VehiculeDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
	
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is VehiculeDetailViewModel vm)
        {
            // La commande Refresh existe maintenant, on peut l'appeler
            if (vm.RefreshCommand.CanExecute(null))
            {
                await vm.RefreshCommand.ExecuteAsync(null);
            }
        }
    }
}