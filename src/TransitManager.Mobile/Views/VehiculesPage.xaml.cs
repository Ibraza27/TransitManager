using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class VehiculesPage : ContentPage
{
    private readonly VehiculesViewModel _viewModel;
    public VehiculesPage(VehiculesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

	protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is VehiculesViewModel vm)
        {
            // On appelle InitializeAsync qui charge tout (filtres + liste)
            await vm.InitializeAsync();
        }
    }
}