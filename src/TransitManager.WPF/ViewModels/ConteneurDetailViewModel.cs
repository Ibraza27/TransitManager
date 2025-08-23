using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using TransitManager.Core.Enums;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.WPF.Messages;
using TransitManager.WPF.Views.Inventaire; // <--- NOUVEAU
using System.Text.Json; // <--- NOUVEAU
using Microsoft.Extensions.DependencyInjection; // <--- NOUVEAU
using TransitManager.WPF.Views.Colis; // <--- NOUVEAU
using TransitManager.WPF.Views;

namespace TransitManager.WPF.ViewModels
{
    public class ConteneurDetailViewModel : BaseViewModel
    {
        private readonly IConteneurService _conteneurService;
        private readonly IColisService _colisService;
        private readonly IVehiculeService _vehiculeService;
        private readonly IClientService _clientService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IMessenger _messenger;
        private readonly IServiceProvider _serviceProvider; // <--- NOUVEAU

        private Conteneur? _conteneur;
        public Conteneur? Conteneur
        {
            get => _conteneur;
            set
            {
                if (_conteneur != null) _conteneur.PropertyChanged -= OnConteneurPropertyChanged;
                SetProperty(ref _conteneur, value);
                if (_conteneur != null) _conteneur.PropertyChanged += OnConteneurPropertyChanged;
            }
        }

        public ObservableCollection<Client> ClientsAffiches { get; } = new();
        public ObservableCollection<Colis> ColisAffectes { get; } = new();
        public ObservableCollection<Vehicule> VehiculesAffectes { get; } = new();

        private string _colisSearchText = string.Empty;
        public string ColisSearchText { get => _colisSearchText; set => SetProperty(ref _colisSearchText, value); }
        public ObservableCollection<Colis> ColisSearchResults { get; } = new();

        private string _vehiculeSearchText = string.Empty;
        public string VehiculeSearchText { get => _vehiculeSearchText; set => SetProperty(ref _vehiculeSearchText, value); }
        public ObservableCollection<Vehicule> VehiculeSearchResults { get; } = new();

        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IAsyncRelayCommand SearchColisCommand { get; }
        public IAsyncRelayCommand<Colis> AddColisCommand { get; }
        public IAsyncRelayCommand<Colis> RemoveColisCommand { get; }
        public IAsyncRelayCommand SearchVehiculesCommand { get; }
        public IAsyncRelayCommand<Vehicule> AddVehiculeCommand { get; }
        public IAsyncRelayCommand<Vehicule> RemoveVehiculeCommand { get; }
        public IAsyncRelayCommand<Colis> OpenInventaireCommand { get; } // <--- NOUVEAU
        public IAsyncRelayCommand<Colis> EditColisInWindowCommand { get; } // <--- NOUVEAU

        public ConteneurDetailViewModel(
            IConteneurService conteneurService, IColisService colisService, IVehiculeService vehiculeService,
            INavigationService navigationService, IDialogService dialogService, IClientService clientService, IMessenger messenger, IServiceProvider serviceProvider)
        {
            _conteneurService = conteneurService;
            _colisService = colisService;
            _vehiculeService = vehiculeService;
            _clientService = clientService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _messenger = messenger;
            _serviceProvider = serviceProvider; // <--- NOUVEAU

            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(() => _navigationService.GoBack());

            SearchColisCommand = new AsyncRelayCommand(SearchColisAsync);
            AddColisCommand = new AsyncRelayCommand<Colis>(AddColisAsync);
            RemoveColisCommand = new AsyncRelayCommand<Colis>(RemoveColisAsync);

            SearchVehiculesCommand = new AsyncRelayCommand(SearchVehiculesAsync);
            AddVehiculeCommand = new AsyncRelayCommand<Vehicule>(AddVehiculeAsync);
            RemoveVehiculeCommand = new AsyncRelayCommand<Vehicule>(RemoveVehiculeAsync);
            
            OpenInventaireCommand = new AsyncRelayCommand<Colis>(OpenInventaireAsync); // <--- NOUVEAU
            EditColisInWindowCommand = new AsyncRelayCommand<Colis>(EditColisInWindowAsync); // <--- NOUVEAU

            ColisAffectes.CollectionChanged += OnAffectationChanged;
            VehiculesAffectes.CollectionChanged += OnAffectationChanged;
        }

        // <--- NOUVELLE MÉTHODE : OUVRE LA FENÊTRE D'INVENTAIRE --- >
        private async Task OpenInventaireAsync(Colis? colis)
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
                
                await _colisService.UpdateAsync(colis);
                await InitializeAsync(Conteneur.Id); // Rafraîchir la vue
            }
        }


		private async Task EditColisInWindowAsync(Colis? colisFromList)
		{
			if (colisFromList == null) return;

			using var scope = _serviceProvider.CreateScope();
			var colisDetailViewModel = scope.ServiceProvider.GetRequiredService<ColisDetailViewModel>();
			
			colisDetailViewModel.SetModalMode();

			// On passe l'ID. Le ViewModel de détail se chargera de tout récupérer proprement.
			await colisDetailViewModel.InitializeAsync(colisFromList.Id);
			
			if (colisDetailViewModel.Colis == null)
			{
				await _dialogService.ShowErrorAsync("Erreur", "Impossible de charger les détails de ce colis.");
				return;
			}

			var window = new DetailHostWindow
			{
				DataContext = colisDetailViewModel,
				Owner = System.Windows.Application.Current.MainWindow,
				Title = $"Modifier le Colis - {colisDetailViewModel.Colis.NumeroReference}"
			};

			colisDetailViewModel.CloseAction = () => window.Close();
			window.ShowDialog();

			if (Conteneur != null)
			{
				await InitializeAsync(Conteneur.Id);
			}
		}

        // ... LE RESTE DU FICHIER RESTE INCHANGÉ ...
        // (CanSave, OnConteneurPropertyChanged, CalculateStatus, etc.)
        private void OnAffectationChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }

        private bool CanSave()
        {
            if (Conteneur == null || IsBusy) return false;
            
            bool isNewAndValid = string.IsNullOrEmpty(Conteneur.CreePar) &&
                                 !string.IsNullOrWhiteSpace(Conteneur.NumeroDossier) &&
                                 !string.IsNullOrWhiteSpace(Conteneur.Destination) &&
                                 !string.IsNullOrWhiteSpace(Conteneur.PaysDestination) &&
                                 Conteneur.DateReception.HasValue;

            return isNewAndValid || !string.IsNullOrEmpty(Conteneur.CreePar);
        }

        private void OnConteneurPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();
            if (Conteneur == null) return;
            var dateProperties = new[]
            {
                nameof(Conteneur.DateReception), nameof(Conteneur.DateChargement), nameof(Conteneur.DateDepart),
                nameof(Conteneur.DateArriveeDestination), nameof(Conteneur.DateDedouanement)
            };
            if (dateProperties.Contains(e.PropertyName))
            {
                Conteneur.Statut = CalculateStatus(Conteneur);
            }
        }
        
        private StatutConteneur CalculateStatus(Conteneur conteneur)
        {
            if (conteneur.DateDedouanement.HasValue) return StatutConteneur.EnDedouanement;
            if (conteneur.DateArriveeDestination.HasValue) return StatutConteneur.Arrive;
            if (conteneur.DateDepart.HasValue) return StatutConteneur.EnTransit;
            if (conteneur.DateChargement.HasValue) return StatutConteneur.EnPreparation;
            if (conteneur.DateReception.HasValue) return StatutConteneur.Reçu;
            return StatutConteneur.Reçu;
        }

        public async Task InitializeAsync(string newMarker)
        {
            if (newMarker == "new")
            {
                Title = "Nouveau Dossier Conteneur";
                Conteneur = new Conteneur { DateReception = DateTime.UtcNow };
            }
        }

        public async Task InitializeAsync(Guid conteneurId)
        {
            Title = "Détails du Dossier Conteneur";
            await ExecuteBusyActionAsync(async () =>
            {
                Conteneur = await _conteneurService.GetByIdAsync(conteneurId);
                if (Conteneur != null)
                {
                    RefreshCollections();
                }
            });
        }
        
        private void RefreshCollections()
        {
            if (Conteneur == null) return;
            ClientsAffiches.Clear();
            foreach(var client in Conteneur.ClientsDistincts) ClientsAffiches.Add(client);
            ColisAffectes.Clear();
            foreach(var colis in Conteneur.Colis) ColisAffectes.Add(colis);
            VehiculesAffectes.Clear();
            foreach(var vehicule in Conteneur.Vehicules) VehiculesAffectes.Add(vehicule);
        }

		private async Task SaveAsync()
		{
			if (Conteneur == null) return;
			if (!CanSave()) 
			{
				await _dialogService.ShowWarningAsync("Validation", "Veuillez remplir tous les champs obligatoires (*).");
				return;
			}

			await ExecuteBusyActionAsync(async () =>
			{
				try
				{
					bool isNew = string.IsNullOrEmpty(Conteneur.CreePar);
					if (isNew)
					{
						await _conteneurService.CreateAsync(Conteneur);
					}
					else
					{
						await _conteneurService.UpdateAsync(Conteneur);
					}
					await _dialogService.ShowInformationAsync("Succès", "Le dossier a été enregistré.");
					_messenger.Send(new ConteneurUpdatedMessage(true));
					_navigationService.GoBack();
				}
				catch (Exception ex)
				{
					await _dialogService.ShowErrorAsync("Erreur d'enregistrement", $"{ex.Message}\n\nDétails: {ex.InnerException?.Message}");
				}
			});
		}
        
        private async Task AddColisAsync(Colis? colis)
        {
            if (colis == null || Conteneur == null) return;
            bool success = await _colisService.AssignToConteneurAsync(colis.Id, Conteneur.Id);
            if (success)
            {
                colis.ConteneurId = Conteneur.Id;
                colis.Statut = StatutColis.Affecte;
                colis.NumeroPlomb = Conteneur.NumeroPlomb;
                ColisAffectes.Add(colis);
                ColisSearchResults.Remove(colis);
                RefreshClientList();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Erreur", "Le colis n'a pas pu être affecté.");
            }
        }
        
        private async Task RemoveColisAsync(Colis? colis)
        {
            if (colis == null) return;
            bool success = await _colisService.RemoveFromConteneurAsync(colis.Id);
            if (success)
            {
                ColisAffectes.Remove(colis);
                RefreshClientList();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Erreur", "Le colis n'a pas pu être retiré.");
            }
        }
        
        private async Task AddVehiculeAsync(Vehicule? vehicule)
        {
            if (vehicule == null || Conteneur == null) return;
            bool success = await _vehiculeService.AssignToConteneurAsync(vehicule.Id, Conteneur.Id);
            if (success)
            {
                vehicule.ConteneurId = Conteneur.Id;
                vehicule.Statut = StatutVehicule.Affecte;
                vehicule.NumeroPlomb = Conteneur.NumeroPlomb;
                VehiculesAffectes.Add(vehicule);
                VehiculeSearchResults.Remove(vehicule);
                RefreshClientList();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Erreur", "Le véhicule n'a pas pu être affecté.");
            }
        }

        private async Task RemoveVehiculeAsync(Vehicule? vehicule)
        {
            if (vehicule == null) return;
            bool success = await _vehiculeService.RemoveFromConteneurAsync(vehicule.Id);
            if (success)
            {
                VehiculesAffectes.Remove(vehicule);
                RefreshClientList();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Erreur", "Le véhicule n'a pas pu être retiré.");
            }
        }
        
        private void RefreshClientList()
        {
            if (Conteneur == null) return;
            var colisClients = ColisAffectes.Select(c => c.Client);
            var vehiculeClients = VehiculesAffectes.Select(v => v.Client);
            var allClients = colisClients.Union(vehiculeClients).Where(c => c != null).Select(c => c!).DistinctBy(c => c.Id).ToList();
            ClientsAffiches.Clear();
            foreach(var client in allClients) ClientsAffiches.Add(client);
        }

        private async Task SearchColisAsync()
        {
            if (string.IsNullOrWhiteSpace(ColisSearchText)) { ColisSearchResults.Clear(); return; }
            var results = await _colisService.SearchAsync(ColisSearchText);
            var unassigned = results.Where(c => c.Statut == StatutColis.EnAttente);
            ColisSearchResults.Clear();
            foreach (var item in unassigned) ColisSearchResults.Add(item);
        }
        private async Task SearchVehiculesAsync()
        {
            if (string.IsNullOrWhiteSpace(VehiculeSearchText)) { VehiculeSearchResults.Clear(); return; }
            var results = await _vehiculeService.SearchAsync(VehiculeSearchText);
            var unassigned = results.Where(v => v.Statut == StatutVehicule.EnAttente);
            VehiculeSearchResults.Clear();
            foreach (var item in unassigned) VehiculeSearchResults.Add(item);
        }
    }
}