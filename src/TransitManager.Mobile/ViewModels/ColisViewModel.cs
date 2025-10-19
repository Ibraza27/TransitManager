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
    public partial class ColisViewModel : ObservableObject
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

        private List<ColisListItemDto> _allColis = new();
        public ObservableCollection<ColisListItemDto> ColisList { get; } = new();

        public ColisViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
            StatutsList.Add("Tous");
            foreach (var status in Enum.GetNames(typeof(StatutColis)))
            {
                StatutsList.Add(status);
            }
        }

        public async Task InitializeAsync()
        {
            await LoadFilterDataAsync();
            await LoadColisAsync();
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
        private async Task LoadColisAsync()
        {
            IsBusy = true;
            try
            {
                _allColis = (await _transitApi.GetColisAsync()).ToList();
                ApplyFilters();
            }
            catch (System.Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les colis : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ApplyFilters()
        {
            IEnumerable<ColisListItemDto> filtered = _allColis;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerms = SearchText.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    filtered = filtered.Where(c =>
                        c.NumeroReference.ToLower().Contains(term) ||
                        c.Designation.ToLower().Contains(term) ||
                        c.ClientNomComplet.ToLower().Contains(term)
                    );
                }
            }

            if (SelectedClient != null)
            {
                filtered = filtered.Where(c => c.ClientNomComplet == SelectedClient.NomComplet);
            }
            if (SelectedConteneur != null)
            {
                filtered = filtered.Where(c => c.ConteneurNumeroDossier == SelectedConteneur.NumeroDossier);
            }
            if (SelectedStatut != "Tous" && Enum.TryParse<StatutColis>(SelectedStatut, out var statut))
            {
                filtered = filtered.Where(c => c.Statut == statut);
            }
            
            ColisList.Clear();
            foreach(var c in filtered) ColisList.Add(c);
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatut = "Tous";
            SelectedClient = null;
            SelectedConteneur = null;
            SelectedDate = null;
            ApplyFilters();
        }
        
        partial void OnSearchTextChanged(string value) => ApplyFilters();
        partial void OnSelectedStatutChanged(string value) => ApplyFilters();
        partial void OnSelectedClientChanged(ClientListItemDto? value) => ApplyFilters();
        partial void OnSelectedConteneurChanged(Conteneur? value) => ApplyFilters();
        partial void OnSelectedDateChanged(DateTime? value) => ApplyFilters();

        [RelayCommand]
        private async Task GoToDetailsAsync(ColisListItemDto colis)
        {
            if (colis == null) return;
            await Shell.Current.GoToAsync($"ColisDetailPage?colisId={colis.Id}");
        }

        [RelayCommand]
        private async Task AddColisAsync()
        {
            await Shell.Current.GoToAsync("AddEditColisPage");
        }
    }
}