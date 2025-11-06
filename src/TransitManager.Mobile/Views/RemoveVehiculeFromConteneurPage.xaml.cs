using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class RemoveVehiculeFromConteneurPage : ContentPage
{
	public RemoveVehiculeFromConteneurPage(RemoveVehiculeFromConteneurViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}