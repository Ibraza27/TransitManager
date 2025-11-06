using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq; 
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ClientIdStr), "clientId")]
    public partial class ClientDetailViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private bool _isBusy;
        
        private Guid _clientId;

        // --- DÉBUT DE LA MODIFICATION 1 ---
        private Client? _client;
        public Client? Client
        {
            get => _client;
            set 
            {
                if (SetProperty(ref _client, value))
                {
                    // Notifier explicitement que les propriétés calculées doivent être mises à jour
                    OnPropertyChanged(nameof(ImpayesColis));
                    OnPropertyChanged(nameof(ImpayesVehicules));
                }
            }
        }
        // --- FIN DE LA MODIFICATION 1 ---

        [ObservableProperty]
        private string _clientIdStr = string.Empty;

        public decimal ImpayesColis => Client?.Colis?.Where(c => c.Actif).Sum(c => c.RestantAPayer) ?? 0m;
        public decimal ImpayesVehicules => Client?.Vehicules?.Where(v => v.Actif).Sum(v => v.RestantAPayer) ?? 0m;

        public ClientDetailViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }
        
        async partial void OnClientIdStrChanged(string value)
        {
            if (Guid.TryParse(value, out _clientId))
            {
                await LoadClientDetailsAsync();
            }
        }

        [RelayCommand]
        private async Task LoadClientDetailsAsync()
        {
            if (IsBusy || _clientId == Guid.Empty) return;
            IsBusy = true;

            try
            {
                Client = await _transitApi.GetClientByIdAsync(_clientId);

                // La notification est maintenant gérée par le setter de la propriété Client
            }
            catch (System.Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les détails du client : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
		[RelayCommand]
		async Task EditAsync()
		{
			if (Client == null) return;
			await Shell.Current.GoToAsync($"AddEditClientPage?clientId={Client.Id}");
		}

		[RelayCommand]
		async Task DeleteAsync()
		{
			if (Client == null) return;

			bool confirm = await Shell.Current.DisplayAlert("Supprimer", $"Êtes-vous sûr de vouloir supprimer {Client.NomComplet} ?", "Oui", "Non");
			if (confirm)
			{
				await _transitApi.DeleteClientAsync(Client.Id);
				await Shell.Current.GoToAsync(".."); 
			}
		}
    }
}