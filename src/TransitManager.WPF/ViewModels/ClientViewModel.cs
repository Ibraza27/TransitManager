using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using TransitManager.Core.Enums;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.WPF.Messages;

namespace TransitManager.WPF.ViewModels
{
    public class ClientViewModel : BaseViewModel, IRecipient<ClientUpdatedMessage>
    {
        private readonly IClientService _clientService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IExportService _exportService;
        private readonly IServiceProvider _serviceProvider;
		private readonly IMessenger _messenger;

        private ObservableCollection<Client> _clients = new();
        private Client? _selectedClient;
        private string _searchText = string.Empty;
        private string _selectedStatus = "Tous";
        private string? _selectedCity;
        private ObservableCollection<string> _cities = new();

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalPages = 1;
        private int _totalClients;

        // Statistiques
        private int _fidelesCount;
        private decimal _totalImpaye;

        #region Propriétés

        public ObservableCollection<Client> Clients
        {
            get => _clients;
            set => SetProperty(ref _clients, value);
        }

        public Client? SelectedClient
        {
            get => _selectedClient;
            set => SetProperty(ref _selectedClient, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // Recherche en temps réel
                    _ = SearchClientsAsync();
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                {
                    _ = LoadClientsAsync();
                }
            }
        }

        public string? SelectedCity
        {
            get => _selectedCity;
            set
            {
                if (SetProperty(ref _selectedCity, value))
                {
                    _ = LoadClientsAsync();
                }
            }
        }

        public ObservableCollection<string> Cities
        {
            get => _cities;
            set => SetProperty(ref _cities, value);
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    OnPropertyChanged(nameof(CanGoPrevious));
                    OnPropertyChanged(nameof(CanGoNext));
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public int TotalClients
        {
            get => _totalClients;
            set => SetProperty(ref _totalClients, value);
        }

        public int FidelesCount
        {
            get => _fidelesCount;
            set => SetProperty(ref _fidelesCount, value);
        }

        public decimal TotalImpaye
        {
            get => _totalImpaye;
            set => SetProperty(ref _totalImpaye, value);
        }

        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;

        #endregion

        // Commandes
        public ICommand NewClientCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }

		// NOUVELLE VERSION CORRIGÉE
		public ClientViewModel(
			IClientService clientService,
			INavigationService navigationService,
			IDialogService dialogService,
			IExportService exportService,
			IServiceProvider serviceProvider,
			IMessenger messenger) // <-- PARAMÈTRE AJOUTÉ
		{
			_clientService = clientService;
			_navigationService = navigationService;
			_dialogService = dialogService;
			_exportService = exportService;
			_serviceProvider = serviceProvider;
			_messenger = messenger; // <-- LIGNE D'INITIALISATION AJOUTÉE
			
			_clientService.ClientStatisticsUpdated += OnClientStatisticsUpdated;

			Title = "Gestion des Clients";

			// Initialiser les commandes
			NewClientCommand = new RelayCommand(NewClient);
			EditCommand = new RelayCommand<Client>(EditClient);
			DeleteCommand = new AsyncRelayCommand<Client>(DeleteClientAsync);
			ViewDetailsCommand = new RelayCommand<Client>(ViewDetails);
			ExportCommand = new AsyncRelayCommand(ExportAsync);
			RefreshCommand = new AsyncRelayCommand(RefreshAsync);
			SearchCommand = new AsyncRelayCommand(LoadClientsAsync);
			PreviousPageCommand = new RelayCommand(PreviousPage, () => CanGoPrevious);
			NextPageCommand = new RelayCommand(NextPage, () => CanGoNext);
			
			_messenger.RegisterAll(this);
		}
		
		private async void OnClientStatisticsUpdated(Guid clientId)
		{
			// L'événement nous donne l'ID du client mis à jour.
			// Pour une liste, le plus simple est de tout recharger.
			await LoadAsync();
		}
		
        public override async Task InitializeAsync()
        {
			ClearFilters();
            // Cette méthode sera appelée à chaque fois que la vue devient active.
            // Elle lance le chargement complet des données.
            await LoadAsync();
        }		

		private void ClearFilters()
		{
			SearchText = string.Empty;
			SelectedStatus = "Tous";
			SelectedCity = null;
		}

		public override async Task LoadAsync()
		{
			await LoadCitiesAsync();
			await LoadClientsAsync();
			await LoadStatisticsAsync();
		}

        private async Task LoadClientsAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                // Étape 1 : Obtenir la liste COMPLÈTE des clients une seule fois.
                var allClients = await _clientService.GetAllAsync();
                
                // Étape 2 : Appliquer tous les filtres en mémoire (c'est plus flexible).
                IEnumerable<Client> filteredClients = allClients;

                // Filtre par statut
                switch (SelectedStatus)
                {
                    case "Actifs":
                        filteredClients = filteredClients.Where(c => c.Actif);
                        break;
                    case "Inactifs":
                        filteredClients = filteredClients.Where(c => !c.Actif);
                        break;
                    case "Clients fidèles":
                        filteredClients = filteredClients.Where(c => c.EstClientFidele);
                        break;
                    case "Avec impayés":
                        filteredClients = filteredClients.Where(c => c.Impayes > 0);
                        break;
                    // Le cas "Tous" ne fait rien, on garde la liste complète.
                }

                // Filtre par ville
                if (!string.IsNullOrEmpty(SelectedCity))
                {
                    filteredClients = filteredClients.Where(c => c.Ville == SelectedCity);
                }

                // Filtre par recherche texte
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTextLower = SearchText.ToLower();
                    filteredClients = filteredClients.Where(c =>
                        (c.CodeClient?.ToLower().Contains(searchTextLower) ?? false) ||
                        (c.NomComplet?.ToLower().Contains(searchTextLower) ?? false) ||
                        (c.TelephonePrincipal?.Contains(searchTextLower) ?? false) ||
                        (c.Email?.ToLower().Contains(searchTextLower) ?? false) ||
                        (c.Ville?.ToLower().Contains(searchTextLower) ?? false)
                    );
                }

                // Pagination
                var finalList = filteredClients.ToList();
                TotalClients = finalList.Count();
                TotalPages = (int)Math.Ceiling(TotalClients / (double)_pageSize);
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                var pagedClients = finalList
                    .Skip((CurrentPage - 1) * _pageSize)
                    .Take(_pageSize);

                Clients = new ObservableCollection<Client>(pagedClients);
            });
        }

		private async Task LoadCitiesAsync()
		{
			var allClients = await _clientService.GetAllAsync();
			var cities = allClients
				.Select(c => c.Ville)
				.Where(v => !string.IsNullOrEmpty(v)) // On filtre les villes nulles ou vides
				.Select(v => v!) // On affirme au compilateur que ce n'est plus null
				.Distinct()
				.OrderBy(v => v)
				.ToList();

			Cities = new ObservableCollection<string>(cities);
		}

        private async Task LoadStatisticsAsync()
        {
            var allClients = await _clientService.GetAllAsync();
            
            FidelesCount = allClients.Count(c => c.EstClientFidele);
            TotalImpaye = await _clientService.GetTotalUnpaidBalanceAsync();
        }

		private void NewClient()
		{
			_navigationService.NavigateTo("ClientDetail", "new");
		}

		private void EditClient(Client? client)
		{
			if (client == null) return;
			_navigationService.NavigateTo("ClientDetail", client.Id);
		}
        private async Task DeleteClientAsync(Client? client)
        {
            if (client == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync(
                "Supprimer le client",
                $"Êtes-vous sûr de vouloir supprimer le client {client.NomComplet} ?\n\n" +
                "Cette action est irréversible.");

            if (confirm)
            {
                try
                {
                    await _clientService.DeleteAsync(client.Id);
                    await RefreshAsync();
                    
                    StatusMessage = $"Client {client.NomComplet} supprimé avec succès.";
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", 
                        $"Impossible de supprimer le client : {ex.Message}");
                }
            }
        }

		private void ViewDetails(Client? client)
		{
			if (client == null) return;
			_navigationService.NavigateTo("ClientDetail", client.Id);
		}

        private async Task ExportAsync()
        {
            var format = await _dialogService.ShowDialogAsync<string>(new ExportOptionsViewModel());
            
            if (string.IsNullOrEmpty(format)) return;

            await ExecuteBusyActionAsync(async () =>
            {
                StatusMessage = "Export en cours...";

                try
                {
                    byte[] data;
                    string fileName;
                    string filter;

                    var allClients = await _clientService.GetAllAsync();

                    switch (format)
                    {
                        case "Excel":
                            data = await _exportService.ExportClientsToExcelAsync(allClients);
                            fileName = $"Clients_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                            filter = "Excel Files (*.xlsx)|*.xlsx";
                            break;

                        case "CSV":
                            var csvPath = await _exportService.ExportToCsvAsync(allClients, $"Clients_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                            data = await System.IO.File.ReadAllBytesAsync(csvPath);
                            fileName = System.IO.Path.GetFileName(csvPath);
                            filter = "CSV Files (*.csv)|*.csv";
                            break;

                        default:
                            return;
                    }

                    var savePath = _dialogService.ShowSaveFileDialog(filter, fileName);
                    if (!string.IsNullOrEmpty(savePath))
                    {
                        await System.IO.File.WriteAllBytesAsync(savePath, data);
                        StatusMessage = $"Export terminé : {System.IO.Path.GetFileName(savePath)}";
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
            CurrentPage = 1;
            SearchText = string.Empty;
            await LoadAsync();
        }

        private async Task SearchClientsAsync()
        {
            CurrentPage = 1;
            await LoadClientsAsync();
        }

        private void PreviousPage()
        {
            if (CanGoPrevious)
            {
                CurrentPage--;
                _ = LoadClientsAsync();
            }
        }

        private void NextPage()
        {
            if (CanGoNext)
            {
                CurrentPage++;
                _ = LoadClientsAsync();
            }
        }
		
		public async void Receive(ClientUpdatedMessage message)
		{
			// Ce message est reçu quand un client a été créé ou modifié.
			// On relance simplement le chargement complet de la vue.
			await LoadAsync();
		}
		
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_clientService.ClientStatisticsUpdated -= OnClientStatisticsUpdated;
			}
			base.Dispose(disposing);
		}
	
		
    }

    // ViewModel pour les options d'export
    public class ExportOptionsViewModel
    {
        public string SelectedFormat { get; set; } = "Excel";
        public List<string> AvailableFormats { get; } = new() { "Excel", "CSV", "PDF" };
    }
	

}