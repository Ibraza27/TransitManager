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
// --- LIGNES À AJOUTER ---
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.Core.Messages;
using System.Text.Json;
using TransitManager.WPF.Views.Inventaire;
using Microsoft.Extensions.DependencyInjection; // Assurez-vous que celui-ci est présent
using TransitManager.WPF.Views; // <--- LIGNE À AJOUTER

namespace TransitManager.WPF.ViewModels
{
    public class ColisViewModel : BaseViewModel, IRecipient<ClientUpdatedMessage>, IRecipient<ConteneurUpdatedMessage>
    {
        #region Services
        private readonly IColisService _colisService;
        private readonly IClientService _clientService;
        private readonly IConteneurService _conteneurService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IExportService _exportService;
        private readonly IMessenger _messenger;
		private readonly IServiceProvider _serviceProvider;
		private readonly IPaiementService _paiementService;
        #endregion

        #region Propriétés
        private ObservableCollection<Colis> _colis = new();
        public ObservableCollection<Colis> Colis { get => _colis; set => SetProperty(ref _colis, value); }
        
        private Colis? _selectedColis;
        public Colis? SelectedColis { get => _selectedColis; set => SetProperty(ref _selectedColis, value); }
        
        private string _searchText = string.Empty;
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) { _ = LoadColisAsync(); } } }

		private bool _showFilters = true;
		public bool ShowFilters { get => _showFilters; set => SetProperty(ref _showFilters, value); }

		private string? _selectedStatut = "Tous";
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

		private Client? _selectedClient;
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
		
        private List<Client> _fullClientsList = new();
		
        public ObservableCollection<Client> ClientsList { get; } = new();

		private Conteneur? _selectedConteneur;
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
		
		
        private List<Conteneur> _fullConteneursList = new();
        public ObservableCollection<Conteneur> ConteneursList { get; } = new();

		private DateTime? _selectedDate;
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
        
        private ObservableCollection<string> _statutsList = new();
        public ObservableCollection<string> StatutsList { get => _statutsList; set => SetProperty(ref _statutsList, value); }

        private int _totalColis;
        public int TotalColis { get => _totalColis; set => SetProperty(ref _totalColis, value); }

        private decimal _volumeTotal;
        public decimal VolumeTotal { get => _volumeTotal; set => SetProperty(ref _volumeTotal, value); }

        private int _totalPieces;
        public int TotalPieces { get => _totalPieces; set => SetProperty(ref _totalPieces, value); }

        private decimal _prixTotalGlobal;
        public decimal PrixTotalGlobal { get => _prixTotalGlobal; set => SetProperty(ref _prixTotalGlobal, value); }

        private decimal _totalPayeGlobal;
        public decimal TotalPayeGlobal { get => _totalPayeGlobal; set => SetProperty(ref _totalPayeGlobal, value); }

        private decimal _totalRestantGlobal;
        public decimal TotalRestantGlobal { get => _totalRestantGlobal; set => SetProperty(ref _totalRestantGlobal, value); }

        #endregion

        #region Commandes
        public IAsyncRelayCommand NewColisCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand SearchCommand { get; }
        public IRelayCommand ClearFiltersCommand { get; }
        public IAsyncRelayCommand ExportCommand { get; }
        public IAsyncRelayCommand<Colis> EditCommand { get; }
        public IAsyncRelayCommand<Colis> DeleteCommand { get; }
		public IAsyncRelayCommand<Colis> OpenInventaireFromListCommand { get; }
		public IAsyncRelayCommand<Colis> ViewClientDetailsInWindowCommand { get; }
        #endregion

		public ColisViewModel(
			IColisService colisService, IClientService clientService, IConteneurService conteneurService, 
			INavigationService navigationService, IDialogService dialogService, IExportService exportService, 
			IMessenger messenger, IServiceProvider serviceProvider, IPaiementService paiementService) // <-- AJOUTER
		{
            _colisService = colisService;
            _clientService = clientService;
            _conteneurService = conteneurService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _exportService = exportService;
			_paiementService = paiementService;
			_clientService.ClientStatisticsUpdated += OnDataShouldRefresh;
            _messenger = messenger; // Ligne ajoutée
			_serviceProvider = serviceProvider;
            Title = "Gestion des Colis / Marchandises";

            NewColisCommand = new AsyncRelayCommand(NewColis);
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            SearchCommand = new AsyncRelayCommand(LoadColisAsync);
            ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync);
            ExportCommand = new AsyncRelayCommand(ExportAsync);
            EditCommand = new AsyncRelayCommand<Colis>(EditColis);
            DeleteCommand = new AsyncRelayCommand<Colis>(DeleteColis);
            OpenInventaireFromListCommand = new AsyncRelayCommand<Colis>(OpenInventaireFromList);
			ViewClientDetailsInWindowCommand = new AsyncRelayCommand<Colis>(ViewClientDetailsInWindowAsync);
			OpenPaiementsWindowCommand = new AsyncRelayCommand<Colis>(OpenPaiementsWindowAsync);

            InitializeStatutsList();
            _messenger.RegisterAll(this);
        }
		
		
		// ##### NOUVELLE COMMANDE #####
		public IAsyncRelayCommand<Colis> OpenPaiementsWindowCommand { get; }

		// ##### NOUVELLE MÉTHODE #####
		private async Task OpenPaiementsWindowAsync(Colis? colis)
		{
			if (colis == null) return;

			using var scope = _serviceProvider.CreateScope();
			var paiementViewModel = scope.ServiceProvider.GetRequiredService<PaiementColisViewModel>();
			await paiementViewModel.InitializeAsync(colis);

			var paiementWindow = new Views.Paiements.PaiementColisView(paiementViewModel)
			{
				Owner = System.Windows.Application.Current.MainWindow
			};

			if (paiementWindow.ShowDialog() == true)
			{
				// LIGNE À SUPPRIMER
				// await LoadAsync(); 

				// ##### NOUVELLE LOGIQUE CI-DESSOUS #####
				// On met à jour directement l'objet dans la collection.
				// INotifyPropertyChanged fera le reste pour mettre à jour l'UI.
				colis.SommePayee = paiementViewModel.TotalValeur;

				// Recalculer les statistiques globales de la liste
				CalculateStatistics();
			}
		}
		
		private async Task ViewClientDetailsInWindowAsync(Colis? colis)
		{
			if (colis?.Client == null) return;

			using var scope = _serviceProvider.CreateScope();
			var clientDetailViewModel = scope.ServiceProvider.GetRequiredService<ClientDetailViewModel>();

			clientDetailViewModel.SetModalMode();
			await clientDetailViewModel.InitializeAsync(colis.ClientId);

			if (clientDetailViewModel.Client == null)
			{
				await _dialogService.ShowErrorAsync("Erreur", "Impossible de charger les détails de ce client.");
				return;
			}

			var window = new DetailHostWindow
			{
				DataContext = clientDetailViewModel,
				Owner = System.Windows.Application.Current.MainWindow,
				Title = $"Détails du Client - {clientDetailViewModel.Client.NomComplet}"
			};

			clientDetailViewModel.CloseAction = () => window.Close();
			window.ShowDialog();
			
			await LoadAsync();
		}
		
        private async Task OpenInventaireFromList(Colis? colis)
        {
            if (colis == null) return;

            var inventaireViewModel = new InventaireViewModel(colis.InventaireJson);
            var inventaireWindow = new InventaireView(inventaireViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (inventaireWindow.ShowDialog() == true)
            {
                colis.InventaireJson = JsonSerializer.Serialize(inventaireViewModel.Items);
                colis.NombrePieces = inventaireViewModel.TotalQuantite;
                colis.ValeurDeclaree = inventaireViewModel.TotalValeur;

                // Sauvegarder directement les changements
                await _colisService.UpdateAsync(colis);
                // Rafraîchir la liste pour voir les nouvelles valeurs
                await LoadColisAsync();
            }
        }		

        public async void Receive(ClientUpdatedMessage message)
        {
            await LoadFilterDataAsync();
        }

        public async void Receive(ConteneurUpdatedMessage message)
        {
            await LoadFilterDataAsync();
        }
        
		public override async Task InitializeAsync()
		{
			// 1. On charge les données des filtres UNE SEULE FOIS.
			await InitializeFiltersAsync();
			
			// 2. On réinitialise les filtres et on charge la liste des colis.
			await ClearFiltersAsync();
		}

		// CETTE MÉTHODE EST MAINTENANT DÉDIÉE AU RECHARGEMENT DE LA LISTE DE COLIS
		public override async Task LoadAsync()
		{
			await ExecuteBusyActionAsync(async () =>
			{
				StatusMessage = "Chargement des colis...";
				await LoadColisAsync();
				StatusMessage = "";
			});
		}
		

		// NOUVELLE MÉTHODE POUR INITIALISER LES FILTRES
		private async Task InitializeFiltersAsync()
		{
			await ExecuteBusyActionAsync(async () =>
			{
				StatusMessage = "Chargement des filtres...";
				await LoadFilterDataAsync();
				StatusMessage = "";
			});
		}

        
        private async Task LoadColisAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var filteredColis = await _colisService.GetAllAsync();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTextLower = SearchText.ToLower();
                    filteredColis = filteredColis.Where(c => 
                        c.AllBarcodes.ToLower().Contains(searchTextLower) ||
                        c.NumeroReference.ToLower().Contains(searchTextLower) ||
						c.Designation.ToLower().Contains(searchTextLower) ||
                        (c.Client?.NomComplet.ToLower().Contains(searchTextLower) == true) ||
                        (c.Conteneur?.NumeroDossier.ToLower().Contains(searchTextLower) == true) ||
                        (c.DestinationFinale.ToLower().Contains(searchTextLower))
                    );
                }

                if (SelectedClient != null) {
                    filteredColis = filteredColis.Where(c => c.ClientId == SelectedClient.Id);
                }
                if (SelectedConteneur != null) {
                    filteredColis = filteredColis.Where(c => c.ConteneurId == SelectedConteneur.Id);
                }
                if (SelectedDate.HasValue) {
                    filteredColis = filteredColis.Where(c => c.DateArrivee.Date == SelectedDate.Value.Date);
                }
                if (!string.IsNullOrEmpty(SelectedStatut) && SelectedStatut != "Tous" && Enum.TryParse<StatutColis>(SelectedStatut, out var statut)) {
                    filteredColis = filteredColis.Where(c => c.Statut == statut);
                }
                
                Colis = new ObservableCollection<Core.Entities.Colis>(filteredColis.ToList());
                CalculateStatistics();
            });
        }
        
        private async Task LoadFilterDataAsync()
        {
            _fullClientsList = (await _clientService.GetActiveClientsAsync()).ToList();
            ClientsList.Clear();
            foreach(var client in _fullClientsList) ClientsList.Add(client);

            _fullConteneursList = (await _conteneurService.GetAllAsync()).ToList();
            ConteneursList.Clear();
            foreach(var conteneur in _fullConteneursList) ConteneursList.Add(conteneur);
        }

        private void InitializeStatutsList() { StatutsList = new ObservableCollection<string>(Enum.GetNames(typeof(StatutColis))); StatutsList.Insert(0, "Tous"); }
        
        private void CalculateStatistics() 
        { 
            TotalColis = Colis.Count; 
            VolumeTotal = Colis.Sum(c => c.Volume);
            TotalPieces = Colis.Sum(c => c.NombrePieces);
            PrixTotalGlobal = Colis.Sum(c => c.PrixTotal);
            TotalPayeGlobal = Colis.Sum(c => c.SommePayee);
            TotalRestantGlobal = Colis.Sum(c => c.RestantAPayer);
        }
        
		// NOUVELLE MÉTHODE ASYNCHRONE
		private async Task ClearFiltersAsync()
		{
			// On modifie les champs privés directement pour ne pas déclencher les rechargements multiples
			SetProperty(ref _searchText, string.Empty, nameof(SearchText));
			SetProperty(ref _selectedClient, null, nameof(SelectedClient));
			SetProperty(ref _selectedConteneur, null, nameof(SelectedConteneur));
			SetProperty(ref _selectedDate, null, nameof(SelectedDate));
			SetProperty(ref _selectedStatut, "Tous", nameof(SelectedStatut));

			// On ne recharge QUE la liste des colis. Les filtres sont déjà chargés.
			await LoadColisAsync();
		}

        private Task NewColis() { _navigationService.NavigateTo("ColisDetail", "new"); return Task.CompletedTask; }
        private Task EditColis(Colis? colis) { if (colis != null) { _navigationService.NavigateTo("ColisDetail", colis.Id); } return Task.CompletedTask; }

        private async Task DeleteColis(Colis? colis)
        {
            if (colis == null) return;
            var confirm = await _dialogService.ShowConfirmationAsync("Supprimer le Colis", $"Êtes-vous sûr de vouloir supprimer le colis {colis.NumeroReference}?");
            if (confirm) {
                await _colisService.DeleteAsync(colis.Id);
                await LoadAsync();
            }
        }
        
        private async Task ExportAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var data = await _exportService.ExportColisToExcelAsync(Colis);
                var savePath = _dialogService.ShowSaveFileDialog("Fichiers Excel (*.xlsx)|*.xlsx", $"Export_Colis_{DateTime.Now:yyyyMMdd}.xlsx");
                if (!string.IsNullOrEmpty(savePath))
                {
                    await System.IO.File.WriteAllBytesAsync(savePath, data);
                    await _dialogService.ShowInformationAsync("Succès", "Exportation réussie.");
                }
            });
        }
		
		// ##### MÉTHODE À AJOUTER DANS LA CLASSE #####
		private async void OnDataShouldRefresh(Guid clientId)
		{
			// Un paiement a changé, on recharge toute la liste pour être à jour.
			await LoadAsync();
		}
		
		// ##### MÉTHODE À AJOUTER À LA FIN DE LA CLASSE #####
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_clientService.ClientStatisticsUpdated -= OnDataShouldRefresh;
			}
			base.Dispose(disposing);
		}
		
    }
}