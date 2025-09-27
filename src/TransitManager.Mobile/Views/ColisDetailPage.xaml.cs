using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ColisDetailPage : ContentPage
{
    public ColisDetailPage(ColisDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}