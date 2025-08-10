using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
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
        private readonly IColisService _colisService;
        private readonly IClientService _clientService;
        private readonly IConteneurService _conteneurService;
        private readonly IBarcodeService _barcodeService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IExportService _exportService;

        private ObservableCollection<Colis> _colis = new();
        private ObservableCollection<Client> _clientsList = new();
        private ObservableCollection<Conteneur> _conteneursList = new();
        private ObservableCollection<string> _statutsList = new();
        
        private Colis? _selectedColis;
        private string _searchText = string.Empty;
        private bool _showFilters;
        private string? _selectedStatut;
        private Client? _selectedClient;
        private Conteneur? _selectedConteneur;
        private DateTime? _selectedDate;

        // Statistiques
        private int _totalColis;
        private decimal _poidsTotal;
        private decimal _volumeTotal;
        private decimal _valeurTotale;
        private bool _hasColisEnAttenteLongue;
        private string _colisEnAttenteLongueMessage = string.Empty;

        #region Propriétés

        public ObservableCollection<Colis> Colis
        {
            get => _colis;
            set => SetProperty(ref _colis, value);
        }

        public ObservableCollection<Client> ClientsList
        {
            get => _clientsList;
            set => SetProperty(ref _clientsList, value);
        }

        public ObservableCollection<Conteneur> ConteneursList
        {
            get => _conteneursList;
            set => SetProperty(ref _conteneursList, value);
        }

        public ObservableCollection<string> StatutsList
        {
            get => _statutsList;
            set => SetProperty(ref _statutsList, value);
        }

        public Colis? SelectedColis
        {
            get => _selectedColis;
            set
            {
                if (SetProperty(ref _selectedColis, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = SearchAsync();
                }
            }
        }

        public bool ShowFilters
        {
            get => _showFilters;
            set => SetProperty(ref _showFilters, value);
        }

        public string? SelectedStatut
        {
            get => _selectedStatut;
            set
            {
                if (SetProperty(ref _selectedStatut, value))
                {
                    _ = LoadColisAsync();
                }
            }
        }

        public Client? SelectedClient
        {
            get => _selectedClient;
            set
            {
                if (SetProperty(ref _selectedClient, value))
                {
                    _ = LoadColisAsync();
                }
            }
        }

        public Conteneur? SelectedConteneur
        {
            get => _selectedConteneur;
            set
            {
                if (SetProperty(ref _selectedConteneur, value))
                {
                    _ = LoadColisAsync();
                }
            }
        }

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                {
                    _ = LoadColisAsync();
                }
            }
        }

        public int TotalColis
        {
            get => _totalColis;
            set => SetProperty(ref _totalColis, value);
        }

        public decimal PoidsTotal
        {
            get => _poidsTotal;
            set => SetProperty(ref _poidsTotal, value);
        }

        public decimal VolumeTotal
        {
            get => _volumeTotal;
            set => SetProperty(ref _volumeTotal, value);
        }

        public decimal ValeurTotale
        {
            get => _valeurTotale;
            set => SetProperty(ref _valeurTotale, value);
        }

        public bool HasColisEnAttenteLongue
        {
            get => _hasColisEnAttenteLongue;
            set => SetProperty(ref _hasColisEnAttenteLongue, value);
        }

        public string ColisEnAttenteLongueMessage
        {
            get => _colisEnAttenteLongueMessage;
            set => SetProperty(ref _colisEnAttenteLongueMessage, value);
        }

        public bool HasSelection => SelectedColis != null;

        #endregion

        // Commandes
        public ICommand NewColisCommand { get; }
        public ICommand ScanCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ScanColisCommand { get; }
        public ICommand PrintLabelCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand AssignToContainerCommand { get; }
        public ICommand PrintLabelsCommand { get; }

        public ColisViewModel(
            IColisService colisService,
            IClientService clientService,
            IConteneurService conteneurService,
            IBarcodeService barcodeService,
            INavigationService navigationService,
            IDialogService dialogService,
            IExportService exportService)
        {
            _colisService = colisService;
            _clientService = clientService;
            _conteneurService = conteneurService;
            _barcodeService = barcodeService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _exportService = exportService;

            Title = "Gestion des Colis";

            // Initialiser les commandes
            NewColisCommand = new AsyncRelayCommand(NewColisAsync);
            ScanCommand = new AsyncRelayCommand(OpenScannerAsync);
            EditCommand = new AsyncRelayCommand<Colis>(EditColisAsync);
            ScanColisCommand = new AsyncRelayCommand<Colis>(ScanColisAsync);
            PrintLabelCommand = new AsyncRelayCommand<Colis>(PrintLabelAsync);
            ExportCommand = new AsyncRelayCommand(ExportAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            AssignToContainerCommand = new AsyncRelayCommand(AssignToContainerAsync);
            PrintLabelsCommand = new AsyncRelayCommand(PrintLabelsAsync);

            InitializeStatutsList();
        }

        public override async Task LoadAsync()
        {
            await LoadClientsAsync();
            await LoadConteneursAsync();
            await LoadColisAsync();
            await CheckColisEnAttenteAsync();
        }

        private void InitializeStatutsList()
        {
            StatutsList = new ObservableCollection<string>(
                Enum.GetNames(typeof(StatutColis))
            );
            StatutsList.Insert(0, "Tous");
        }

        private async Task LoadClientsAsync()
        {
            var clients = await _clientService.GetActiveClientsAsync();
            ClientsList = new ObservableCollection<Client>(clients);
        }

        private async Task LoadConteneursAsync()
        {
            var conteneurs = await _conteneurService.GetActiveAsync();
            ConteneursList = new ObservableCollection<Conteneur>(conteneurs);
        }

        private async Task LoadColisAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                IEnumerable<Colis> colis;

                // Appliquer les filtres
                if (!string.IsNullOrEmpty(SelectedStatut) && SelectedStatut != "Tous")
                {
                    if (Enum.TryParse<StatutColis>(SelectedStatut, out var statut))
                    {
                        colis = await _colisService.GetByStatusAsync(statut);
                    }
                    else
                    {
                        colis = await _colisService.GetAllAsync();
                    }
                }
                else
                {
                    colis = await _colisService.GetAllAsync();
                }

                // Filtrer par client
                if (SelectedClient != null)
                {
                    colis = colis.Where(c => c.ClientId == SelectedClient.Id);
                }

                // Filtrer par conteneur
                if (SelectedConteneur != null)
                {
                    colis = colis.Where(c => c.ConteneurId == SelectedConteneur.Id);
                }

                // Filtrer par date
                if (SelectedDate.HasValue)
                {
                    var startDate = SelectedDate.Value.Date;
                    var endDate = startDate.AddDays(1);
                    colis = colis.Where(c => c.DateArrivee >= startDate && c.DateArrivee < endDate);
                }

                // Appliquer la recherche
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    colis = await _colisService.SearchAsync(SearchText);
                }

                // Ajouter des propriétés calculées
                var colisList = colis.ToList();
                foreach (var c in colisList)
                {
                    // Marquer les colis en attente depuis longtemps
                    if (c.Statut == StatutColis.EnAttente && 
                        (DateTime.Now - c.DateArrivee).TotalDays > 7)
                    {
                        // Ajouter une propriété dynamique (utiliser un ViewModel wrapper si nécessaire)
                    }
                }

                Colis = new ObservableCollection<Colis>(colisList);

                // Calculer les statistiques
                await CalculateStatisticsAsync();
            });
        }

        private async Task CalculateStatisticsAsync()
        {
            await Task.Run(() =>
            {
                TotalColis = Colis.Count;
                PoidsTotal = Colis.Sum(c => c.Poids);
                VolumeTotal = Colis.Sum(c => c.Volume);
                ValeurTotale = Colis.Sum(c => c.ValeurDeclaree);
            });
        }

        private async Task CheckColisEnAttenteAsync()
        {
            var colisEnAttente = await _colisService.GetColisWaitingLongTimeAsync(7);
            var count = colisEnAttente.Count();
            
            HasColisEnAttenteLongue = count > 0;
            ColisEnAttenteLongueMessage = count > 0 
                ? $"⚠️ {count} colis en attente depuis plus de 7 jours" 
                : string.Empty;
        }

        private async Task NewColisAsync()
        {
            _navigationService.NavigateTo("ColisDetail", "new");
            await Task.CompletedTask;
        }

        private async Task OpenScannerAsync()
        {
            _navigationService.NavigateTo("Scanner");
            await Task.CompletedTask;
        }

        private async Task EditColisAsync(Colis? colis)
        {
            if (colis == null) return;
            _navigationService.NavigateTo("ColisDetail", colis.Id);
            await Task.CompletedTask;
        }

        private async Task ScanColisAsync(Colis? colis)
        {
            if (colis == null) return;

            await ExecuteBusyActionAsync(async () =>
            {
                try
                {
                    var location = await _dialogService.ShowInputAsync(
                        "Scanner le colis",
                        "Entrez la localisation actuelle :",
                        colis.LocalisationActuelle ?? "Entrepôt principal"
                    );

                    if (!string.IsNullOrEmpty(location))
                    {
                        await _colisService.ScanAsync(colis.NumeroReference, location);
                        await RefreshAsync();
                        
                        StatusMessage = $"Colis {colis.NumeroReference} scanné avec succès.";
                    }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", ex.Message);
                }
            });
        }

        private async Task PrintLabelAsync(Colis? colis)
        {
            if (colis == null) return;

            await ExecuteBusyActionAsync(async () =>
            {
                try
                {
                    await _barcodeService.GenerateLabelAsync(colis);
                    StatusMessage = $"Étiquette générée pour le colis {colis.NumeroReference}";
                    
                    // TODO: Lancer l'impression
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur d'impression", ex.Message);
                }
            });
        }

        private async Task ExportAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                try
                {
                    var data = await _exportService.ExportColisToExcelAsync(Colis);
                    var fileName = $"Colis_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    
                    var savePath = _dialogService.ShowSaveFileDialog(
                        "Excel Files (*.xlsx)|*.xlsx", 
                        fileName
                    );
                    
                    if (!string.IsNullOrEmpty(savePath))
                    {
                        await System.IO.File.WriteAllBytesAsync(savePath, data);
                        StatusMessage = "Export terminé avec succès.";
                    }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur d'export", ex.Message);
                }
            });
        }

        private async Task RefreshAsync()
        {
            ClearFilters();
            await LoadAsync();
        }

        private async Task SearchAsync()
        {
            await LoadColisAsync();
        }

        private void ClearFilters()
        {
            SelectedStatut = "Tous";
            SelectedClient = null;
            SelectedConteneur = null;
            SelectedDate = null;
            SearchText = string.Empty;
        }

        private async Task AssignToContainerAsync()
        {
            if (SelectedColis == null)
            {
                await _dialogService.ShowWarningAsync(
                    "Aucune sélection",
                    "Veuillez sélectionner au moins un colis."
                );
                return;
            }

            // TODO: Implémenter la boîte de dialogue de sélection de conteneur
            var conteneur = await _dialogService.ShowDialogAsync<Conteneur>(
                new SelectConteneurViewModel(_conteneurService)
            );

            if (conteneur != null)
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    try
                    {
                        await _colisService.AssignToConteneurAsync(SelectedColis.Id, conteneur.Id);
                        await RefreshAsync();
                        
                        StatusMessage = $"Colis affecté au conteneur {conteneur.NumeroDossier}";
                    }
                    catch (Exception ex)
                    {
                        await _dialogService.ShowErrorAsync("Erreur", ex.Message);
                    }
                });
            }
        }

        private async Task PrintLabelsAsync()
        {
            if (!HasSelection)
            {
                await _dialogService.ShowWarningAsync(
                    "Aucune sélection",
                    "Veuillez sélectionner au moins un colis."
                );
                return;
            }

            // TODO: Implémenter l'impression multiple d'étiquettes
            await Task.CompletedTask;
        }
    }

    // ViewModel pour la sélection de conteneur
    public class SelectConteneurViewModel : BaseViewModel
    {
        private readonly IConteneurService _conteneurService;
        private ObservableCollection<Conteneur> _conteneurs = new();
        private Conteneur? _selectedConteneur;

        public ObservableCollection<Conteneur> Conteneurs
        {
            get => _conteneurs;
            set => SetProperty(ref _conteneurs, value);
        }

        public Conteneur? SelectedConteneur
        {
            get => _selectedConteneur;
            set => SetProperty(ref _selectedConteneur, value);
        }

        public SelectConteneurViewModel(IConteneurService conteneurService)
        {
            _conteneurService = conteneurService;
            _ = LoadConteneursAsync();
        }

        private async Task LoadConteneursAsync()
        {
            var conteneurs = await _conteneurService.GetActiveAsync();
            Conteneurs = new ObservableCollection<Conteneur>(
                conteneurs.Where(c => c.PeutRecevoirColis)
            );
        }
    }
}