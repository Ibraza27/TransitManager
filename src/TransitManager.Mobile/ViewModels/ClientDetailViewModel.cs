using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    // La propriété qui reçoit le paramètre de l'URL est maintenant une string
    [QueryProperty(nameof(ClientIdStr), "clientId")]
    public partial class ClientDetailViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private bool _isBusy;

        // On garde une propriété Guid pour un usage interne, mais elle n'est pas liée à la navigation
        private Guid _clientId;

        [ObservableProperty]
        private Client? _client;

        // C'est cette propriété de type string qui va recevoir la valeur de l'URL
        [ObservableProperty]
        private string _clientIdStr = string.Empty;

        public ClientDetailViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        // La méthode de notification est maintenant liée au changement de la string
        async partial void OnClientIdStrChanged(string value)
        {
            // On convertit la chaîne reçue en Guid
            if (Guid.TryParse(value, out _clientId))
            {
                // Et on lance le chargement des données
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
                // On utilise notre variable _clientId (qui est bien un Guid) pour appeler l'API
                Client = await _transitApi.GetClientByIdAsync(_clientId);
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
    }
}