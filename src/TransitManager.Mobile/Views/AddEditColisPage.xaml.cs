using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class AddEditColisPage : ContentPage
{
    public AddEditColisPage(AddEditColisViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // --- AJOUTER CETTE MÉTHODE ---
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AddEditColisViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}