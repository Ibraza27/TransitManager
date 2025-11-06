using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class AddEditPaiementPage : ContentPage
{
    public AddEditPaiementPage(AddEditPaiementViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}