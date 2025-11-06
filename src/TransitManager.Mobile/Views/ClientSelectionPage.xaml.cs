using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ClientSelectionPage : ContentPage
{
	public ClientSelectionPage(ClientSelectionViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ClientSelectionViewModel vm)
        {
            await vm.LoadClientsCommand.ExecuteAsync(null);
        }
    }
}