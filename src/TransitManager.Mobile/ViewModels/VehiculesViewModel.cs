using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    public partial class VehiculesViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _searchText = string.Empty;

        public ObservableCollection<string> StatutsList { get; } = new();
        public ObservableCollection<ClientListItemDto> ClientsList { get; } = new();
        public ObservableCollection<Conteneur> ConteneursList { get; } = new();

        [ObservableProperty] private string _selectedStatut = "Tous";
        [ObservableProperty] private ClientListItemDto? _selectedClient;
        [ObservableProperty] private Conteneur? _selectedConteneur;
        [ObservableProperty] private DateTime? _selectedDate;

        private List<VehiculeListItemDto> _allVehicules = new();
        public ObservableCollection<VehiculeListItemDto> VehiculesList { get; } = new();

        public VehiculesViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
            StatutsList.Add("Tous");
            foreach (var status in Enum.GetNames(typeof(StatutVehicule)))
            {
                StatutsList.Add(status);
            }
        }

        public async Task InitializeAsync()
        {
            await LoadFilterDataAsync();
            await LoadVehiculesAsync();
        }

        private async Task LoadFilterDataAsync()
        {
            var clients = await _transitApi.GetClientsAsync();
            ClientsList.Clear();
            foreach(var c in clients) ClientsList.Add(c);

            var conteneurs = await _transitApi.GetConteneursAsync();
            ConteneursList.Clear();
            foreach(var c in conteneurs) ConteneursList.Add(c);
        }

        [RelayCommand]
        private async Task LoadVehiculesAsync()
        {
            IsBusy = true;
            try
            {
                _allVehicules = (await _transitApi.GetVehiculesAsync()).ToList();
                ApplyFilters();
            }
            catch (System.Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les véhicules : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ApplyFilters()
        {
            IEnumerable<VehiculeListItemDto> filtered = _allVehicules;

            // Filtre par recherche texte
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerms = SearchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    filtered = filtered.Where(v =>
                        v.Immatriculation.ToLower().Contains(term) ||
                        v.Marque.ToLower().Contains(term) ||
                        v.Modele.ToLower().Contains(term) ||
                        v.ClientNomComplet.ToLower().Contains(term)
                    );
                }
            }

			// Autres filtres
            if (SelectedClient != null)
            {
                filtered = filtered.Where(v => v.ClientNomComplet == SelectedClient.NomComplet);
            }
            if (SelectedStatut != "Tous" && Enum.TryParse<StatutVehicule>(SelectedStatut, out var statut))
            {
                filtered = filtered.Where(v => v.Statut == statut);
            }
            
            if (SelectedConteneur != null)
            {
                filtered = filtered.Where(v => v.ConteneurNumeroDossier == SelectedConteneur.NumeroDossier);
            }
            
            // --- CORRECTION 1 : ACTIVER LE FILTRE PAR DATE ---
            if (SelectedDate.HasValue)
            {
                filtered = filtered.Where(v => v.DateCreation.Date == SelectedDate.Value.Date);
            }
            
            VehiculesList.Clear();
            foreach(var v in filtered) VehiculesList.Add(v);
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatut = "Tous";
            SelectedClient = null;
            // --- CORRECTION 2 : RÉINITIALISER LES NOUVEAUX FILTRES ---
            SelectedConteneur = null;
            SelectedDate = null; 
            
            ApplyFilters(); // Appeler ApplyFilters une seule fois à la fin
        }

        // --- Méthodes partielles pour la réactivité ---
        partial void OnSearchTextChanged(string value) => ApplyFilters();
        partial void OnSelectedStatutChanged(string value) => ApplyFilters();
        partial void OnSelectedClientChanged(ClientListItemDto? value) => ApplyFilters();
        partial void OnSelectedConteneurChanged(Conteneur? value) => ApplyFilters();
        partial void OnSelectedDateChanged(DateTime? value) => ApplyFilters();

        [RelayCommand]
        private async Task GoToDetailsAsync(VehiculeListItemDto vehicule)
        {
            if (vehicule == null) return;
            await Shell.Current.GoToAsync($"VehiculeDetailPage?vehiculeId={vehicule.Id}");
        }

        [RelayCommand]
        private async Task AddVehiculeAsync()
        {
            await Shell.Current.GoToAsync("AddEditVehiculePage");
        }
    }
}