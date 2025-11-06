using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class AddEditClientPage : ContentPage
{
    public AddEditClientPage(AddEditClientViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}