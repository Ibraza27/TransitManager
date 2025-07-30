using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using TransitManager.Core.Enums;

namespace TransitManager.WPF.ViewModels
{
    public class ConteneurViewModel : BaseViewModel
    {
        private readonly IConteneurService _conteneurService;
        private readonly IColisService _colisService;
        private readonly IClientService _clientService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IExportService _exportService;

        private ObservableCollection<Conteneur> _conteneurs = new();
        private ObservableCollection<string> _destinations = new();
        private ObservableCollection<string> _statusList = new();
        
        private Conteneur? _selectedConteneur;
        private string _searchText = string.Empty;
        private string? _selectedDestination;
        private string? _selectedStatus;
        private bool _showOnlyOpen;
        private DateTime? _dateDepart;

        // Statistiques
        private int _totalConteneurs;
        private int _conteneursOuverts;
        private int _conteneursEnTransit;
        private decimal _tauxRemplissageMoyen;
        private Dictionary<string, int> _repartitionParDestination = new();

        #region Propriétés

        public ObservableCollection<Conteneur> Conteneurs
        {
            get => _conteneurs;
            set => SetProperty(ref _conteneurs, value);
        }

        public ObservableCollection<string> Destinations
        {
            get => _destinations;
            set => SetProperty(ref _destinations, value);
        }

        public ObservableCollection<string> StatusList
        {
            get => _statusList;
            set => SetProperty(ref _statusList, value);
        }

        public Conteneur? SelectedConteneur
        {
            get => _selectedConteneur;
            set
            {
                if (SetProperty(ref _selectedConteneur, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    RefreshCommands();
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

        public string? SelectedDestination
        {
            get => _selectedDestination;
            set
            {
                if (SetProperty(ref _selectedDestination, value))
                {
                    _ = LoadConteneursAsync();
                }
            }
        }

        public string? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    _ = LoadConteneursAsync();
                }
            }
        }

        public bool ShowOnlyOpen
        {
            get => _showOnlyOpen;
            set
            {
                if (SetProperty(ref _showOnlyOpen, value))
                {
                    _ = LoadConteneursAsync();
                }
            }
        }

        public DateTime? DateDepart
        {
            get => _dateDepart;
            set
            {
                if (SetProperty(ref _dateDepart, value))
                {
                    _ = LoadConteneursAsync();
                }
            }
        }

        public int TotalConteneurs
        {
            get => _totalConteneurs;
            set => SetProperty(ref _totalConteneurs, value);
        }

        public int ConteneursOuverts
        {
            get => _conteneursOuverts;
            set => SetProperty(ref _conteneursOuverts, value);
        }

        public int ConteneursEnTransit
        {
            get => _conteneursEnTransit;
            set => SetProperty(ref _conteneursEnTransit, value);
        }

        public decimal TauxRemplissageMoyen
        {
            get => _tauxRemplissageMoyen;
            set => SetProperty(ref _tauxRemplissageMoyen, value);
        }

        public Dictionary<string, int> RepartitionParDestination
        {
            get => _repartitionParDestination;
            set => SetProperty(ref _repartitionParDestination, value);
        }

        public bool HasSelection => SelectedConteneur != null;
        public bool CanMarkDeparture => SelectedConteneur?.Statut == StatutConteneur.Ouvert || 
                                        SelectedConteneur?.Statut == StatutConteneur.EnPreparation;
        public bool CanMarkArrival => SelectedConteneur?.Statut == StatutConteneur.EnTransit;
        public bool CanClose => SelectedConteneur?.Statut == StatutConteneur.Livre;

        #endregion

        // Commandes
        public ICommand NewConteneurCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand MarkDepartureCommand { get; }
        public ICommand MarkArrivalCommand { get; }
        public ICommand CloseConteneurCommand { get; }
        public ICommand PrintManifestCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public ConteneurViewModel(
            IConteneurService conteneurService,
            IColisService colisService,
            IClientService clientService,
            INavigationService navigationService,
            IDialogService dialogService,
            IExportService exportService)
        {
            _conteneurService = conteneurService;
            _colisService = colisService;
            _clientService = clientService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _exportService = exportService;

            Title = "Gestion des Conteneurs";

            // Initialiser les commandes
            NewConteneurCommand = new AsyncRelayCommand(NewConteneurAsync);
            EditCommand = new AsyncRelayCommand<Conteneur>(EditConteneurAsync);
            DeleteCommand = new AsyncRelayCommand<Conteneur>(DeleteConteneurAsync);
            ViewDetailsCommand = new AsyncRelayCommand<Conteneur>(ViewDetailsAsync);
            MarkDepartureCommand = new AsyncRelayCommand(MarkDepartureAsync, () => CanMarkDeparture);
            MarkArrivalCommand = new AsyncRelayCommand(MarkArrivalAsync, () => CanMarkArrival);
            CloseConteneurCommand = new AsyncRelayCommand(CloseConteneurAsync, () => CanClose);
            PrintManifestCommand = new AsyncRelayCommand<Conteneur>(PrintManifestAsync);
            ExportCommand = new AsyncRelayCommand(ExportAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            InitializeStatusList();
        }

        public override async Task LoadAsync()
        {
            await LoadDestinationsAsync();
            await LoadConteneursAsync();
            await LoadStatisticsAsync();
        }

        private void InitializeStatusList()
        {
            StatusList = new ObservableCollection<string>(
                Enum.GetNames(typeof(StatutConteneur))
            );
            StatusList.Insert(0, "Tous");
        }

        private async Task LoadDestinationsAsync()
        {
            var destinations = await _conteneurService.GetAllDestinationsAsync();
            Destinations = new ObservableCollection<string>(destinations);
            Destinations.Insert(0, "Toutes");
        }

        private async Task LoadConteneursAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                IEnumerable<Conteneur> conteneurs;

                // Filtrer par statut
                if (ShowOnlyOpen)
                {
                    conteneurs = await _conteneurService.GetOpenConteneursAsync();
                }
                else if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Tous")
                {
                    if (Enum.TryParse<StatutConteneur>(SelectedStatus, out var statut))
                    {
                        conteneurs = await _conteneurService.GetByStatusAsync(statut);
                    }
                    else
                    {
                        conteneurs = await _conteneurService.GetAllAsync();
                    }
                }
                else
                {
                    conteneurs = await _conteneurService.GetAllAsync();
                }

                // Filtrer par destination
                if (!string.IsNullOrEmpty(SelectedDestination) && SelectedDestination != "Toutes")
                {
                    conteneurs = await _conteneurService.GetByDestinationAsync(SelectedDestination);
                }

                // Filtrer par date de départ
                if (DateDepart.HasValue)
                {
                    conteneurs = conteneurs.Where(c => 
                        c.DateDepartPrevue.HasValue && 
                        c.DateDepartPrevue.Value.Date == DateDepart.Value.Date);
                }

                // Appliquer la recherche
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    conteneurs = conteneurs.Where(c =>
                        c.NumeroDossier.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        c.Destination.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        (c.Transporteur != null && c.Transporteur.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    );
                }

                Conteneurs = new ObservableCollection<Conteneur>(conteneurs);
            });
        }

        private async Task LoadStatisticsAsync()
        {
            await Task.Run(async () =>
            {
                TotalConteneurs = await _conteneurService.GetActiveCountAsync();
                ConteneursOuverts = Conteneurs.Count(c => c.Statut == StatutConteneur.Ouvert);
                ConteneursEnTransit = Conteneurs.Count(c => c.Statut == StatutConteneur.EnTransit);
                TauxRemplissageMoyen = await _conteneurService.GetAverageFillingRateAsync();
                RepartitionParDestination = await _conteneurService.GetStatsByDestinationAsync();
            });
        }

        private async Task NewConteneurAsync()
        {
            _navigationService.NavigateTo("ConteneurDetail", "new");
            await Task.CompletedTask;
        }

        private async Task EditConteneurAsync(Conteneur? conteneur)
        {
            if (conteneur == null) return;
            _navigationService.NavigateTo("ConteneurDetail", conteneur.Id);
            await Task.CompletedTask;
        }

        private async Task DeleteConteneurAsync(Conteneur? conteneur)
        {
            if (conteneur == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                "Supprimer le conteneur",
                $"Êtes-vous sûr de vouloir supprimer le conteneur {conteneur.NumeroDossier} ?\n\n" +
                "Cette action est irréversible."
            );

            if (confirm)
            {
                try
                {
                    await _conteneurService.DeleteAsync(conteneur.Id);
                    await RefreshAsync();
                    StatusMessage = $"Conteneur {conteneur.NumeroDossier} supprimé.";
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", ex.Message);
                }
            }
        }

        private async Task ViewDetailsAsync(Conteneur? conteneur)
        {
            if (conteneur == null) return;
            _navigationService.NavigateTo("ConteneurDetail", conteneur.Id);
            await Task.CompletedTask;
        }

        private async Task MarkDepartureAsync()
        {
            if (SelectedConteneur == null) return;

            var date = await _dialogService.ShowInputAsync(
                "Départ du conteneur",
                "Date et heure de départ (JJ/MM/AAAA HH:MM) :",
                DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            );

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var departureDate))
            {
                try
                {
                    await _conteneurService.SetDepartureAsync(SelectedConteneur.Id, departureDate);
                    await RefreshAsync();
                    StatusMessage = $"Conteneur {SelectedConteneur.NumeroDossier} marqué comme parti.";
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", ex.Message);
                }
            }
        }

        private async Task MarkArrivalAsync()
        {
            if (SelectedConteneur == null) return;

            var date = await _dialogService.ShowInputAsync(
                "Arrivée du conteneur",
                "Date et heure d'arrivée (JJ/MM/AAAA HH:MM) :",
                DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            );

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var arrivalDate))
            {
                try
                {
                    await _conteneurService.SetArrivalAsync(SelectedConteneur.Id, arrivalDate);
                    await RefreshAsync();
                    StatusMessage = $"Conteneur {SelectedConteneur.NumeroDossier} marqué comme arrivé.";
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", ex.Message);
                }
            }
        }

        private async Task CloseConteneurAsync()
        {
            if (SelectedConteneur == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                "Clôturer le conteneur",
                $"Êtes-vous sûr de vouloir clôturer le conteneur {SelectedConteneur.NumeroDossier} ?\n\n" +
                "Cette action finalisera le dossier et calculera la rentabilité."
            );

            if (confirm)
            {
                try
                {
                    await _conteneurService.CloseConteneurAsync(SelectedConteneur.Id);
                    await RefreshAsync();
                    StatusMessage = $"Conteneur {SelectedConteneur.NumeroDossier} clôturé.";
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", ex.Message);
                }
            }
        }

        private async Task PrintManifestAsync(Conteneur? conteneur)
        {
            if (conteneur == null) return;

            await ExecuteBusyActionAsync(async () =>
            {
                try
                {
                    var manifest = await _exportService.ExportConteneurManifestAsync(conteneur);
                    var fileName = $"Manifeste_{conteneur.NumeroDossier}_{DateTime.Now:yyyyMMdd}.pdf";
                    
                    var savePath = _dialogService.ShowSaveFileDialog(
                        "PDF Files (*.pdf)|*.pdf",
                        fileName
                    );

                    if (!string.IsNullOrEmpty(savePath))
                    {
                        await System.IO.File.WriteAllBytesAsync(savePath, manifest);
                        StatusMessage = "Manifeste généré avec succès.";
                        
                        // Ouvrir le PDF
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = savePath,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", ex.Message);
                }
            });
        }

        private async Task ExportAsync()
        {
            // TODO: Implémenter l'export des conteneurs
            await Task.CompletedTask;
        }

        private async Task RefreshAsync()
        {
            ClearFilters();
            await LoadAsync();
        }

        private async Task SearchAsync()
        {
            await LoadConteneursAsync();
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedDestination = "Toutes";
            SelectedStatus = "Tous";
            ShowOnlyOpen = false;
            DateDepart = null;
        }

        protected override void RefreshCommands()
        {
            base.RefreshCommands();
            
            (MarkDepartureCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (MarkArrivalCommand as IRelayCommand)?.NotifyCanExecuteChanged();
            (CloseConteneurCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        }
    }
}