using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class ColisDetailPage : ContentPage
{
    public ColisDetailPage(ColisDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // --- DÉBUT DE LA MODIFICATION ---
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // On s'assure que le ViewModel est prêt et on appelle sa commande de rafraîchissement
        if (BindingContext is ColisDetailViewModel vm && vm.RefreshCommand.CanExecute(null))
        {
            await vm.RefreshCommand.ExecuteAsync(null);
        }
    }
    // --- FIN DE LA MODIFICATION ---
}