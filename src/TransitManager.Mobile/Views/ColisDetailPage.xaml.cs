using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ColisDetailPage : ContentPage
{
    public ColisDetailPage(ColisDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ColisDetailViewModel vm && vm.RefreshCommand.CanExecute(null))
        {
            await vm.RefreshCommand.ExecuteAsync(null);
        }
    }
}