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
using TransitManager.Core.DTOs;

namespace TransitManager.WPF.ViewModels
{
	public partial class ColisViewModel : BaseViewModel, 
		IRecipient<ClientUpdatedMessage>, 
		IRecipient<ConteneurUpdatedMessage>,
		IRecipient<EntityTotalPaidUpdatedMessage>
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
			
			await paiementViewModel.InitializeAsync(colis.Id, colis.ClientId, colis.PrixTotal);

			var paiementWindow = new Views.Paiements.PaiementColisView(paiementViewModel)
			{
				Owner = System.Windows.Application.Current.MainWindow
			};

			if (paiementWindow.ShowDialog() == true)
			{
				colis.SommePayee = paiementViewModel.TotalValeur;
                try
                {
                    // Créer le DTO pour la mise à jour
                    var dto = new UpdateColisDto 
                    { 
                        Id = colis.Id,
                        ClientId = colis.ClientId,
                        Designation = colis.Designation,
                        DestinationFinale = colis.DestinationFinale,
                        Barcodes = colis.Barcodes.Select(b => b.Value).ToList(),
                        NombrePieces = colis.NombrePieces,
                        Volume = colis.Volume,
                        ValeurDeclaree = colis.ValeurDeclaree,
                        PrixTotal = colis.PrixTotal,
                        SommePayee = colis.SommePayee, // <-- La valeur mise à jour
                        Destinataire = colis.Destinataire,
                        TelephoneDestinataire = colis.TelephoneDestinataire,
                        LivraisonADomicile = colis.LivraisonADomicile,
                        AdresseLivraison = colis.AdresseLivraison,
                        EstFragile = colis.EstFragile,
                        ManipulationSpeciale = colis.ManipulationSpeciale,
                        InstructionsSpeciales = colis.InstructionsSpeciales,
                        Type = colis.Type,
                        TypeEnvoi = colis.TypeEnvoi,
                        ConteneurId = colis.ConteneurId,
                        Statut = colis.Statut
                    };
                    await _colisService.UpdateAsync(colis.Id, dto);
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur de sauvegarde", $"Impossible de mettre à jour le total payé pour le colis : {ex.Message}");
                }
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
            var inventaireWindow = new Views.Inventaire.InventaireView(inventaireViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (inventaireWindow.ShowDialog() == true)
            {
                colis.InventaireJson = JsonSerializer.Serialize(inventaireViewModel.Items);
                colis.NombrePieces = inventaireViewModel.TotalQuantite;
                colis.ValeurDeclaree = inventaireViewModel.TotalValeur;

                var dto = new UpdateColisDto
                {
                    Id = colis.Id,
                    ClientId = colis.ClientId,
                    Designation = colis.Designation,
                    DestinationFinale = colis.DestinationFinale,
                    Barcodes = colis.Barcodes.Select(b => b.Value).ToList(),
                    NombrePieces = colis.NombrePieces, // Mis à jour
                    Volume = colis.Volume,
                    ValeurDeclaree = colis.ValeurDeclaree, // Mis à jour
                    PrixTotal = colis.PrixTotal,
                    SommePayee = colis.SommePayee,
                    Destinataire = colis.Destinataire,
                    TelephoneDestinataire = colis.TelephoneDestinataire,
                    LivraisonADomicile = colis.LivraisonADomicile,
                    AdresseLivraison = colis.AdresseLivraison,
                    EstFragile = colis.EstFragile,
                    ManipulationSpeciale = colis.ManipulationSpeciale,
                    InstructionsSpeciales = colis.InstructionsSpeciales,
                    Type = colis.Type,
                    TypeEnvoi = colis.TypeEnvoi,
                    ConteneurId = colis.ConteneurId,
                    Statut = colis.Statut,
                    InventaireJson = colis.InventaireJson // Mis à jour
                };

                await _colisService.UpdateAsync(colis.Id, dto);
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
        
        public void Receive(EntityTotalPaidUpdatedMessage message)
        {
            // On cherche si le colis concerné est dans la liste actuellement affichée
            var colisToUpdate = Colis.FirstOrDefault(c => c.Id == message.EntityId);
            
            if (colisToUpdate != null)
            {
                // On met à jour la valeur directement dans l'objet de la collection
                // WPF détectera le changement via INotifyPropertyChanged de l'entité Colis
                colisToUpdate.SommePayee = message.NewTotalPaid;
                
                // Optionnel : Recalculer les totaux globaux du bas de page
                CalculateStatistics();
            }
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
                IEnumerable<Core.Entities.Colis> filteredColis;
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filteredColis = await _colisService.SearchAsync(SearchText);
                }
                else
                {
                    filteredColis = await _colisService.GetAllAsync();
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