using TransitManager.Mobile.ViewModels;

namespace TransitManager.Mobile.Views;

public partial class EtatDesLieuxPage : ContentPage
{
    public EtatDesLieuxPage(EtatDesLieuxViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        DamageGraphicsView.Drawable = viewModel; 
    }

    // --- AJOUTER CETTE MÉTHODE ---
	protected override void OnAppearing()
	{
		base.OnAppearing();
		// On s'assure que le dessin se fait après le premier layout
		Dispatcher.Dispatch(() => DamageGraphicsView.Invalidate());
	}
}