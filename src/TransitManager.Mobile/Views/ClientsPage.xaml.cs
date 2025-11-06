using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ClientsPage : ContentPage
{
    private readonly ClientsViewModel _viewModel;

    public ClientsPage(ClientsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_viewModel != null && !_viewModel.IsDataLoaded) // On ne charge que la première fois
		{
			 await _viewModel.LoadClientsCommand.ExecuteAsync(null);
		}
		else if (_viewModel != null) // Les fois suivantes, on force le rechargement
		{
			// Forcer le rechargement si on revient d'une autre page
			await _viewModel.LoadClientsCommand.ExecuteAsync(null);
		}
	}
}