using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class AddColisToConteneurPage : ContentPage
{
	public AddColisToConteneurPage(AddColisToConteneurViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}