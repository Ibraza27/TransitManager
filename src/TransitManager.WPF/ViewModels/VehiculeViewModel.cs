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
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.WPF.Messages;
using Microsoft.Extensions.DependencyInjection; // Assurez-vous que celui-ci est présent
using TransitManager.WPF.Views; // <--- LIGNE À AJOUTER

namespace TransitManager.WPF.ViewModels
{
    public class VehiculeViewModel : BaseViewModel
    {
        private readonly IVehiculeService _vehiculeService;
        private readonly IClientService _clientService;
        private readonly IConteneurService _conteneurService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
		private readonly IMessenger _messenger;
		private readonly IServiceProvider _serviceProvider;
		private readonly IPaiementService _paiementService; 

        private ObservableCollection<Vehicule> _vehicules = new();
        public ObservableCollection<Vehicule> Vehicules { get => _vehicules; set => SetProperty(ref _vehicules, value); }
		
        private DateTime? _selectedDate;
        public DateTime? SelectedDate { get => _selectedDate; set { if (SetProperty(ref _selectedDate, value)) { _ = LoadVehiculesAsync(); } } }

        private Vehicule? _selectedVehicule;
        public Vehicule? SelectedVehicule { get => _selectedVehicule; set => SetProperty(ref _selectedVehicule, value); }

        private string _searchText = string.Empty;
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) { _ = LoadVehiculesAsync(); } } }

        private bool _showFilters = true;
        public bool ShowFilters { get => _showFilters; set => SetProperty(ref _showFilters, value); }
        
        private string? _selectedStatut = "Tous";
        public string? SelectedStatut { get => _selectedStatut; set { if (SetProperty(ref _selectedStatut, value)) { _ = LoadVehiculesAsync(); } } }
        
        private Client? _selectedClient;
        public Client? SelectedClient { get => _selectedClient; set { if (SetProperty(ref _selectedClient, value)) { _ = LoadVehiculesAsync(); } } }
        private List<Client> _fullClientsList = new();
        public ObservableCollection<Client> ClientsList { get; } = new();

        private Conteneur? _selectedConteneur;
        public Conteneur? SelectedConteneur { get => _selectedConteneur; set { if (SetProperty(ref _selectedConteneur, value)) { _ = LoadVehiculesAsync(); } } }
        private List<Conteneur> _fullConteneursList = new();
        public ObservableCollection<Conteneur> ConteneursList { get; } = new();
        
        public ObservableCollection<string> StatutsList { get; } = new();
        
        private decimal _prixTotalGlobal;
        public decimal PrixTotalGlobal { get => _prixTotalGlobal; set => SetProperty(ref _prixTotalGlobal, value); }

        private decimal _totalPayeGlobal;
        public decimal TotalPayeGlobal { get => _totalPayeGlobal; set => SetProperty(ref _totalPayeGlobal, value); }

        private decimal _totalRestantGlobal;
        public decimal TotalRestantGlobal { get => _totalRestantGlobal; set => SetProperty(ref _totalRestantGlobal, value); }
		
		private int _totalVehicules;
		public int TotalVehicules { get => _totalVehicules; set => SetProperty(ref _totalVehicules, value); }

        public IAsyncRelayCommand NewVehiculeCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IRelayCommand ClearFiltersCommand { get; }
        public IAsyncRelayCommand<Vehicule> EditCommand { get; }
        public IAsyncRelayCommand<Vehicule> DeleteCommand { get; }
		public IAsyncRelayCommand<Vehicule> ViewClientDetailsInWindowCommand { get; }

        public VehiculeViewModel(
			IVehiculeService vehiculeService, IClientService clientService, IConteneurService conteneurService, 
			INavigationService navigationService, IDialogService dialogService, IMessenger messenger,
			IServiceProvider serviceProvider, IPaiementService paiementService)
        {
            _vehiculeService = vehiculeService;
            _clientService = clientService;
            _conteneurService = conteneurService;
            _navigationService = navigationService;
            _dialogService = dialogService;
			_messenger = messenger;
			_serviceProvider = serviceProvider;
			_paiementService = paiementService;
			_messenger.RegisterAll(this);
            Title = "Gestion des Véhicules";

            NewVehiculeCommand = new AsyncRelayCommand(NewVehicule);
            RefreshCommand = new AsyncRelayCommand(LoadAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            EditCommand = new AsyncRelayCommand<Vehicule>(EditVehicule);
            DeleteCommand = new AsyncRelayCommand<Vehicule>(DeleteVehicule);
			ViewClientDetailsInWindowCommand = new AsyncRelayCommand<Vehicule>(ViewClientDetailsInWindowAsync);
			OpenPaiementsWindowCommand = new AsyncRelayCommand<Vehicule>(OpenPaiementsWindowAsync);
            
            StatutsList = new ObservableCollection<string>(Enum.GetNames(typeof(StatutVehicule)));
            StatutsList.Insert(0, "Tous");
        }
		
		// ##### NOUVELLE COMMANDE #####
		public IAsyncRelayCommand<Vehicule> OpenPaiementsWindowCommand { get; }

		// ##### NOUVELLE MÉTHODE #####
		private async Task OpenPaiementsWindowAsync(Vehicule? vehicule)
		{
			if (vehicule == null) return;

			using var scope = _serviceProvider.CreateScope();
			var paiementViewModel = scope.ServiceProvider.GetRequiredService<PaiementVehiculeViewModel>();
			await paiementViewModel.InitializeAsync(vehicule);

			var paiementWindow = new Views.Paiements.PaiementVehiculeView(paiementViewModel)
			{
				Owner = System.Windows.Application.Current.MainWindow
			};

			if (paiementWindow.ShowDialog() == true)
			{
				await LoadAsync(); // Rafraîchir
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
			ClearFilters();
			await LoadAsync();
		}

        public override async Task LoadAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                await LoadFilterDataAsync();
                await LoadVehiculesAsync();
            });
        }

		private async Task ViewClientDetailsInWindowAsync(Vehicule? vehicule)
		{
			if (vehicule?.Client == null) return;

			using var scope = _serviceProvider.CreateScope();
			var clientDetailViewModel = scope.ServiceProvider.GetRequiredService<ClientDetailViewModel>();

			// Activer le mode modal et fournir l'action de fermeture
			clientDetailViewModel.SetModalMode();
			await clientDetailViewModel.InitializeAsync(vehicule.ClientId);

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

			// Rafraîchir la liste au cas où des infos auraient changé
			await LoadAsync();
		}

        private async Task LoadVehiculesAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var allVehicules = await _vehiculeService.GetAllAsync();
                IEnumerable<Vehicule> filteredVehicules = allVehicules;

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchTextLower = SearchText.ToLower();
                    filteredVehicules = filteredVehicules.Where(v => 
                        (v.Immatriculation.ToLower().Contains(searchTextLower)) ||
                        (v.Client?.NomComplet.ToLower().Contains(searchTextLower) == true) ||
                        (v.Conteneur?.NumeroDossier.ToLower().Contains(searchTextLower) == true) ||
                        (v.DestinationFinale.ToLower().Contains(searchTextLower))
                    );
                }

                if (SelectedClient != null) {
                    filteredVehicules = filteredVehicules.Where(v => v.ClientId == SelectedClient.Id);
                }
                if (SelectedConteneur != null) {
                    filteredVehicules = filteredVehicules.Where(v => v.ConteneurId == SelectedConteneur.Id);
                }
                // Filtre par date ajouté
                if (SelectedDate.HasValue) {
                    filteredVehicules = filteredVehicules.Where(c => c.DateCreation.Date == SelectedDate.Value.Date);
                }
                if (SelectedStatut != "Tous" && Enum.TryParse<StatutVehicule>(SelectedStatut, out var statut)) {
                    filteredVehicules = filteredVehicules.Where(v => v.Statut == statut);
                }
                
                Vehicules = new ObservableCollection<Vehicule>(filteredVehicules.ToList());
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

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedClient = null;
            SelectedConteneur = null;
            SelectedStatut = "Tous";
			SelectedDate = null;
            _ = LoadVehiculesAsync();
        }

        private void CalculateStatistics()
        {
			TotalVehicules = Vehicules.Count; 
            PrixTotalGlobal = Vehicules.Sum(v => v.PrixTotal);
            TotalPayeGlobal = Vehicules.Sum(v => v.SommePayee);
            TotalRestantGlobal = Vehicules.Sum(v => v.RestantAPayer);
        }

        private Task NewVehicule()
        {
            _navigationService.NavigateTo("VehiculeDetail", "new");
            return Task.CompletedTask;
        }

        private Task EditVehicule(Vehicule? vehicule)
        {
            if (vehicule != null)
            {
                _navigationService.NavigateTo("VehiculeDetail", vehicule.Id);
            }
            return Task.CompletedTask;
        }

        private async Task DeleteVehicule(Vehicule? vehicule)
        {
            if (vehicule == null) return;
            var confirm = await _dialogService.ShowConfirmationAsync("Supprimer le Véhicule", 
                $"Êtes-vous sûr de vouloir supprimer le véhicule {vehicule.Marque} {vehicule.Modele} ({vehicule.Immatriculation})?");
            
            if (confirm)
            {
                await _vehiculeService.DeleteAsync(vehicule.Id);
                await LoadVehiculesAsync();
            }
        }
    }
}