using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ConteneurPage : ContentPage
{
	public ConteneurPage(ConteneurViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if(BindingContext is ConteneurViewModel vm)
        {
            await vm.LoadConteneursCommand.ExecuteAsync(null);
        }
    }
}