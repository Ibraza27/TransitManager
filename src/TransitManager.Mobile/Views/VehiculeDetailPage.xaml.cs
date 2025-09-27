using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class VehiculeDetailPage : ContentPage
{
    public VehiculeDetailPage(VehiculeDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}