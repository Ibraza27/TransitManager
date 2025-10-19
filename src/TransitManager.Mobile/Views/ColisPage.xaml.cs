using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ColisPage : ContentPage
{
    private readonly ColisViewModel _viewModel;
    public ColisPage(ColisViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

	protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ColisViewModel vm)
        {
            // InitializeAsync charge à la fois les filtres et la liste
            await vm.InitializeAsync();
        }
    }
}