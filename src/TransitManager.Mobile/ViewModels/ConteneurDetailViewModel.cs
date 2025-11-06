using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ConteneurId), "conteneurId")]
    public partial class ConteneurDetailViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private string _conteneurId = string.Empty;

        [ObservableProperty]
        private Conteneur? _conteneur;

        [ObservableProperty]
        private bool _isBusy;
        
        #region Statistiques
        [ObservableProperty] private int _totalColis;
        [ObservableProperty] private decimal _totalPrixColis;
        [ObservableProperty] private decimal _totalPayeColis;
        [ObservableProperty] private decimal _totalRestantColis;
        
        [ObservableProperty] private int _totalVehicules;
        [ObservableProperty] private decimal _totalPrixVehicules;
        [ObservableProperty] private decimal _totalPayeVehicules;
        [ObservableProperty] private decimal _totalRestantVehicules;
        
        public int TotalClientsDistincts => Conteneur?.ClientsDistincts?.Count() ?? 0;
        public decimal TotalGlobalRestant => TotalRestantColis + TotalRestantVehicules;
        #endregion

        public ConteneurDetailViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        async partial void OnConteneurIdChanged(string value)
        {
            if (Guid.TryParse(value, out var id))
            {
                await LoadConteneurDetailsAsync(id);
            }
        }

        [RelayCommand]
        private async Task LoadConteneurDetailsAsync(Guid? id = null)
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var finalId = id ?? Guid.Parse(ConteneurId);
                Conteneur = await _transitApi.GetConteneurByIdAsync(finalId);
                CalculateStatistics();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les détails du conteneur : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private void CalculateStatistics()
        {
            if (Conteneur == null) return;

            TotalColis = Conteneur.Colis.Count;
            TotalPrixColis = Conteneur.Colis.Sum(c => c.PrixTotal);
            TotalPayeColis = Conteneur.Colis.Sum(c => c.SommePayee);
            TotalRestantColis = TotalPrixColis - TotalPayeColis;

            TotalVehicules = Conteneur.Vehicules.Count;
            TotalPrixVehicules = Conteneur.Vehicules.Sum(v => v.PrixTotal);
            TotalPayeVehicules = Conteneur.Vehicules.Sum(v => v.SommePayee);
            TotalRestantVehicules = TotalPrixVehicules - TotalPayeVehicules;
            
            OnPropertyChanged(nameof(TotalClientsDistincts));
            OnPropertyChanged(nameof(TotalGlobalRestant));
        }

        [RelayCommand]
        private async Task GoToEditAsync()
        {
            if (Conteneur == null) return;
            await Shell.Current.GoToAsync($"AddEditConteneurPage?conteneurId={Conteneur.Id}");
        }

        [RelayCommand]
        private async Task GoToAddColisAsync()
        {
            if (Conteneur == null) return;
            await Shell.Current.GoToAsync($"AddColisToConteneurPage?conteneurId={Conteneur.Id}");
        }

        [RelayCommand]
        private async Task GoToAddVehiculesAsync()
        {
            if (Conteneur == null) return;
            await Shell.Current.GoToAsync($"AddVehiculeToConteneurPage?conteneurId={Conteneur.Id}");
        }

        [RelayCommand]
        private async Task GoToRemoveColisAsync()
        {
            if (Conteneur == null) return;
            await Shell.Current.GoToAsync($"RemoveColisFromConteneurPage?conteneurId={Conteneur.Id}");
        }

        [RelayCommand]
        private async Task GoToRemoveVehiculesAsync()
        {
            if (Conteneur == null) return;
            await Shell.Current.GoToAsync($"RemoveVehiculeFromConteneurPage?conteneurId={Conteneur.Id}");
        }
		

		[RelayCommand]
		private async Task DeleteConteneurAsync()
		{
			if (Conteneur == null) return;

			bool confirm = await Shell.Current.DisplayAlert(
				"Confirmation", 
				$"Voulez-vous vraiment supprimer le conteneur {Conteneur.NumeroDossier} ? Cette action est irréversible.", 
				"Oui, Supprimer", 
				"Non");
			
			if (!confirm) return;

			IsBusy = true;
			try
			{
				// --- DÉBUT DE LA CORRECTION : Activer le code ---
				await _transitApi.DeleteConteneurAsync(Conteneur.Id);
				await Shell.Current.GoToAsync(".."); // Revenir à la page de liste
				// --- FIN DE LA CORRECTION ---
			}
			catch (Exception ex)
			{
				await Shell.Current.DisplayAlert("Erreur", $"La suppression a échoué : {ex.Message}", "OK");
			}
			finally
			{
				IsBusy = false;
			}
		}
		
    }
}