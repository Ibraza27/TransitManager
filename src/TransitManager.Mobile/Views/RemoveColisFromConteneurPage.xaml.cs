using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class RemoveColisFromConteneurPage : ContentPage
{
	public RemoveColisFromConteneurPage(RemoveColisFromConteneurViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}