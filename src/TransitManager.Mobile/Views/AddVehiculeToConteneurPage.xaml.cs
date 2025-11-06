using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class AddVehiculeToConteneurPage : ContentPage
{
	public AddVehiculeToConteneurPage(AddVehiculeToConteneurViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}