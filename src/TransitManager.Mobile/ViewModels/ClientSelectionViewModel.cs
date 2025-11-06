using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    public partial class ClientSelectionViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;
        private List<Client> _allClients = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ObservableCollection<Client> FilteredClients { get; } = new();

        public ClientSelectionViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        [RelayCommand]
        private async Task LoadClientsAsync()
        {
            if (IsBusy || _allClients.Any()) return; // Ne charge qu'une seule fois
            IsBusy = true;
            try
            {
                var clients = await _transitApi.GetClientsAsync();
                _allClients = clients.OrderBy(c => c.Nom).ThenBy(c => c.Prenom).ToList();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les clients : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<Client> filtered = _allClients;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(c => 
                    c.NomComplet.ToLower().Contains(search) ||
                    (c.TelephonePrincipal?.Contains(search) ?? false)
                );
            }

            FilteredClients.Clear();
            foreach(var client in filtered)
            {
                FilteredClients.Add(client);
            }
        }

        [RelayCommand]
        private async Task SelectClientAsync(Client? client)
        {
            if (client == null) return;

            // On prépare le paramètre de retour
            var navigationParameter = new Dictionary<string, object>
            {
                { "SelectedClient", client } // On passe l'objet Client complet
            };
            
            // On retourne à la page précédente en passant le paramètre
            await Shell.Current.GoToAsync("..", navigationParameter);
        }
    }
}