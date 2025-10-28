using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class InventairePage : ContentPage
{
	public InventairePage(InventaireViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}