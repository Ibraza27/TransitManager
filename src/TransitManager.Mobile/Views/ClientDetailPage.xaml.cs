using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ClientDetailPage : ContentPage
{
    public ClientDetailPage(ClientDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}