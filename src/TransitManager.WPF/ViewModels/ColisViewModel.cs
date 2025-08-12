using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;

namespace TransitManager.WPF.ViewModels
{
    public class ColisViewModel : BaseViewModel
    {
        #region Services injectés
        private readonly IColisService _colisService;
        private readonly IClientService _clientService;
        private readonly IConteneurService _conteneurService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IExportService _exportService;
        #endregion

        #region Champs privés et Propriétés publiques

        private ObservableCollection<Colis> _colis = new();
        public ObservableCollection<Colis> Colis { get => _colis; set => SetProperty(ref _colis, value); }

        private Colis? _selectedColis;
        public Colis? SelectedColis { get => _selectedColis; set => SetProperty(ref _selectedColis, value); }
        
        // --- Filtres ---
        private string _searchText = string.Empty;
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) { _ = LoadColisAsync(); } } }

        private bool _showFilters;
        public bool ShowFilters { get => _showFilters; set => SetProperty(ref _showFilters, value); }

        private string? _selectedStatut = "Tous";
        public string? SelectedStatut { get => _selectedStatut; set { if (SetProperty(ref _selectedStatut, value)) { _ = LoadColisAsync(); } } }

        private Client? _selectedClient;
        public Client? SelectedClient { get => _selectedClient; set { if (SetProperty(ref _selectedClient, value)) { _ = LoadColisAsync(); } } }

        private Conteneur? _selectedConteneur;
        public Conteneur? SelectedConteneur { get => _selectedConteneur; set { if (SetProperty(ref _selectedConteneur, value)) { _ = LoadColisAsync(); } } }
        
        private DateTime? _selectedDate;
        public DateTime? SelectedDate { get => _selectedDate; set { if (SetProperty(ref _selectedDate, value)) { _ = LoadColisAsync(); } } }
        
        // --- Listes pour les ComboBox de filtres ---
        private ObservableCollection<Client> _clientsList = new();
        public ObservableCollection<Client> ClientsList { get => _clientsList; set => SetProperty(ref _clientsList, value); }
        
        private ObservableCollection<Conteneur> _conteneursList = new();
        public ObservableCollection<Conteneur> ConteneursList { get => _conteneursList; set => SetProperty(ref _conteneursList, value); }

        private ObservableCollection<string> _statutsList = new();
        public ObservableCollection<string> StatutsList { get => _statutsList; set => SetProperty(ref _statutsList, value); }

        // --- Statistiques ---
        private int _totalColis;
        public int TotalColis { get => _totalColis; set => SetProperty(ref _totalColis, value); }

        private decimal _poidsTotal;
        public decimal PoidsTotal { get => _poidsTotal; set => SetProperty(ref _poidsTotal, value); }

        private decimal _volumeTotal;
        public decimal VolumeTotal { get => _volumeTotal; set => SetProperty(ref _volumeTotal, value); }

        #endregion

        #region Commandes
        public IAsyncRelayCommand NewColisCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearFiltersCommand { get; }
        public IAsyncRelayCommand ExportCommand { get; }
        public IAsyncRelayCommand<Colis> EditCommand { get; }
        public IAsyncRelayCommand<Colis> DeleteCommand { get; }
        #endregion

        public ColisViewModel(
            IColisService colisService, 
            IClientService clientService, 
            IConteneurService conteneurService, 
            INavigationService navigationService, 
            IDialogService dialogService, 
            IExportService exportService)
        {
            _colisService = colisService;
            _clientService = clientService;
            _conteneurService = conteneurService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _exportService = exportService;

            Title = "Gestion des Colis / Marchandises";
            
            NewColisCommand = new AsyncRelayCommand(NewColis);
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            SearchCommand = new AsyncRelayCommand(LoadColisAsync); // Le bouton recherche relance le filtre
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExportCommand = new AsyncRelayCommand(ExportAsync);
            EditCommand = new AsyncRelayCommand<Colis>(EditColis);
            DeleteCommand = new AsyncRelayCommand<Colis>(DeleteColis);

            InitializeStatutsList();
        }

        public override Task InitializeAsync()
        {
            // Cette méthode est appelée à chaque fois que l'utilisateur navigue vers cet onglet
            return LoadAsync();
        }

        public async Task LoadAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                StatusMessage = "Chargement des données...";
                var clientsTask = LoadClientsForFilterAsync();
                var conteneursTask = LoadConteneursForFilterAsync();
                await Task.WhenAll(clientsTask, conteneursTask);
                await LoadColisAsync();
                StatusMessage = "";
            });
        }
        
        private async Task LoadColisAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                IEnumerable<Core.Entities.Colis> filteredColis;

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filteredColis = await _colisService.SearchAsync(SearchText);
                }
                else
                {
                    filteredColis = await _colisService.GetAllAsync();
                }

                if (!string.IsNullOrEmpty(SelectedStatut) && SelectedStatut != "Tous" && Enum.TryParse<StatutColis>(SelectedStatut, out var statut))
                {
                    filteredColis = filteredColis.Where(c => c.Statut == statut);
                }
                if (SelectedClient != null)
                {
                    filteredColis = filteredColis.Where(c => c.ClientId == SelectedClient.Id);
                }
                if (SelectedConteneur != null)
                {
                    filteredColis = filteredColis.Where(c => c.ConteneurId == SelectedConteneur.Id);
                }
                if (SelectedDate.HasValue)
                {
                    filteredColis = filteredColis.Where(c => c.DateArrivee.Date == SelectedDate.Value.Date);
                }
                
                Colis = new ObservableCollection<Core.Entities.Colis>(filteredColis.ToList());
                CalculateStatistics();
            });
        }
        
        private async Task LoadClientsForFilterAsync()
        {
            ClientsList = new ObservableCollection<Client>(await _clientService.GetActiveClientsAsync());
        }

        private async Task LoadConteneursForFilterAsync()
        {
            ConteneursList = new ObservableCollection<Conteneur>(await _conteneurService.GetOpenConteneursAsync());
        }

        private void InitializeStatutsList()
        {
            StatutsList = new ObservableCollection<string>(Enum.GetNames(typeof(StatutColis)));
            StatutsList.Insert(0, "Tous");
        }

        private void CalculateStatistics()
        {
            TotalColis = Colis.Count;
            PoidsTotal = Colis.Sum(c => c.Poids);
            VolumeTotal = Colis.Sum(c => c.Volume);
        }
        
        private void ClearFilters()
        {
            // Réinitialiser les propriétés déclenchera automatiquement le rechargement grâce aux setters
            SelectedStatut = "Tous";
            SelectedClient = null;
            SelectedConteneur = null;
            SelectedDate = null;
            SearchText = string.Empty; 
        }

        private Task NewColis()
        {
            _navigationService.NavigateTo("ColisDetail", "new");
            return Task.CompletedTask;
        }

        private Task EditColis(Colis? colis)
        {
            if (colis != null)
            {
                _navigationService.NavigateTo("ColisDetail", colis.Id);
            }
            return Task.CompletedTask;
        }

        private async Task DeleteColis(Colis? colis)
        {
            if (colis == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync("Supprimer le Colis", $"Êtes-vous sûr de vouloir supprimer le colis avec la référence {colis.NumeroReference}?");
            
            if (confirm)
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    try
                    {
                        await _colisService.DeleteAsync(colis.Id);
                        StatusMessage = "Colis supprimé avec succès.";
                        await LoadColisAsync(); // Recharger la liste pour refléter la suppression
                    }
                    catch (Exception ex)
                    {
                        await _dialogService.ShowErrorAsync("Erreur de suppression", ex.Message);
                    }
                });
            }
        }
        
        private async Task ExportAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                try
                {
                    var data = await _exportService.ExportColisToExcelAsync(Colis);
                    var savePath = _dialogService.ShowSaveFileDialog("Fichiers Excel (*.xlsx)|*.xlsx", $"Export_Colis_{DateTime.Now:yyyyMMdd}.xlsx");
                    if (!string.IsNullOrEmpty(savePath))
                    {
                        await System.IO.File.WriteAllBytesAsync(savePath, data);
                        await _dialogService.ShowInformationAsync("Succès", "Les données ont été exportées avec succès.");
                    }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur d'exportation", ex.Message);
                }
            });
        }
    }
}