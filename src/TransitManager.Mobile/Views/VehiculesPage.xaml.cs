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
		if (_viewModel != null)
		{
			await _viewModel.LoadVehiculesCommand.ExecuteAsync(null);
		}
	}
}