using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class AddEditVehiculePage : ContentPage
{
    public AddEditVehiculePage(AddEditVehiculeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AddEditVehiculeViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}