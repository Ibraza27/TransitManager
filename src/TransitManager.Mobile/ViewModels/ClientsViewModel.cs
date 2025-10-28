using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    public partial class ClientsViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;
        
        [ObservableProperty]
        private bool _isBusy;

        // --- DÉBUT DE LA MODIFICATION ---
        private List<Client> _allClients = new();
        public ObservableCollection<Client> Clients { get; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ObservableCollection<string> StatutsList { get; } = new() { "Tous", "Actifs", "Inactifs", "Fidèles", "Avec Impayés" };
        public ObservableCollection<string> CitiesList { get; } = new();
        
        [ObservableProperty] private string _selectedStatus = "Tous";
        [ObservableProperty] private string? _selectedCity;
        // --- FIN DE LA MODIFICATION ---

        public ClientsViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }
		
		public bool IsDataLoaded { get; private set; }

        [RelayCommand]
        private async Task LoadClientsAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                // On charge toutes les données une seule fois
                var clients = await _transitApi.GetClientsAsync();
                _allClients = clients.ToList();
                
                // On peuple la liste des villes pour le filtre
                var cities = _allClients.Select(c => c.Ville).Where(v => !string.IsNullOrEmpty(v)).Distinct().OrderBy(v => v);
                CitiesList.Clear();
                foreach(var city in cities) CitiesList.Add(city!);

                ApplyFilters(); // Appliquer les filtres initiaux
				IsDataLoaded = true;
            }
            catch (System.Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les clients : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // --- DÉBUT DE L'AJOUT DE NOUVELLES MÉTHODES ---
        partial void OnSearchTextChanged(string value) => ApplyFilters();
        partial void OnSelectedStatusChanged(string value) => ApplyFilters();
        partial void OnSelectedCityChanged(string? value) => ApplyFilters();

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatus = "Tous";
            SelectedCity = null;
            // ApplyFilters est déjà appelé par les setters, pas besoin de l'appeler ici.
        }

        private void ApplyFilters()
        {
            IEnumerable<Client> filtered = _allClients;

            // Filtre par texte de recherche
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(c => 
                    c.NomComplet.ToLower().Contains(search) ||
                    (c.CodeClient?.ToLower().Contains(search) ?? false) ||
                    (c.TelephonePrincipal?.Contains(search) ?? false) ||
                    (c.Ville?.ToLower().Contains(search) ?? false)
                );
            }

            // Filtre par statut
            switch (SelectedStatus)
            {
                case "Actifs": filtered = filtered.Where(c => c.Actif); break;
                case "Inactifs": filtered = filtered.Where(c => !c.Actif); break;
                case "Fidèles": filtered = filtered.Where(c => c.EstClientFidele); break;
                case "Avec Impayés": filtered = filtered.Where(c => c.Impayes > 0); break;
            }

            // Filtre par ville
            if (!string.IsNullOrEmpty(SelectedCity))
            {
                filtered = filtered.Where(c => c.Ville == SelectedCity);
            }

            Clients.Clear();
            foreach(var client in filtered)
            {
                Clients.Add(client);
            }
        }
        // --- FIN DE L'AJOUT DE NOUVELLES MÉTHODES ---
		
        [RelayCommand]
        private async Task GoToDetailsAsync(Client client)
        {
            if (client == null) return;
            
            await Shell.Current.GoToAsync($"ClientDetailPage?clientId={client.Id}");
        }

		[RelayCommand]
		private async Task AddClientAsync()
		{
			await Shell.Current.GoToAsync("AddEditClientPage");
		}
    }
}