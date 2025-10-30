using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class AddEditConteneurPage : ContentPage
{
	public AddEditConteneurPage(AddEditConteneurViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}