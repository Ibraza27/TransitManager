using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ConteneurDetailPage : ContentPage
{
	public ConteneurDetailPage(ConteneurDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if(BindingContext is ConteneurDetailViewModel vm)
        {
            await vm.LoadConteneurDetailsCommand.ExecuteAsync(null);
        }
    }
}