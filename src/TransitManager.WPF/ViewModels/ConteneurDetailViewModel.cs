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

        public ConteneurDetailViewModel(
            IConteneurService conteneurService, IColisService colisService, IVehiculeService vehiculeService,
            INavigationService navigationService, IDialogService dialogService, IClientService clientService)
        {
            _conteneurService = conteneurService;
            _colisService = colisService;
            _vehiculeService = vehiculeService;
            _clientService = clientService;
            _navigationService = navigationService;
            _dialogService = dialogService;

            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(() => _navigationService.GoBack());

            SearchColisCommand = new AsyncRelayCommand(SearchColisAsync);
            AddColisCommand = new AsyncRelayCommand<Colis>(AddColisAsync);
            RemoveColisCommand = new AsyncRelayCommand<Colis>(RemoveColisAsync);

            SearchVehiculesCommand = new AsyncRelayCommand(SearchVehiculesAsync);
            AddVehiculeCommand = new AsyncRelayCommand<Vehicule>(AddVehiculeAsync);
            RemoveVehiculeCommand = new AsyncRelayCommand<Vehicule>(RemoveVehiculeAsync);

            ColisAffectes.CollectionChanged += OnAffectationChanged;
            VehiculesAffectes.CollectionChanged += OnAffectationChanged;
        }

        private void OnAffectationChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }

        private bool CanSave()
        {
            if (Conteneur == null || IsBusy) return false;
            
            // La sauvegarde est possible si l'objet a changé depuis son chargement initial
            // ou si c'est un nouvel objet avec les champs requis remplis.
            bool isNewAndValid = string.IsNullOrEmpty(Conteneur.CreePar) &&
                                 !string.IsNullOrWhiteSpace(Conteneur.NumeroDossier) &&
                                 !string.IsNullOrWhiteSpace(Conteneur.Destination) &&
                                 !string.IsNullOrWhiteSpace(Conteneur.PaysDestination) &&
                                 Conteneur.DateReception.HasValue;

            // Pour l'instant, on active toujours si les champs sont bons pour simplifier
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

			// La validation CanSave a déjà été faite
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
						// On passe directement l'objet de l'UI. Le service se chargera du reste.
						await _conteneurService.UpdateAsync(Conteneur);
					}
					await _dialogService.ShowInformationAsync("Succès", "Le dossier a été enregistré.");
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

        // Le reste des méthodes ne change pas
        #region Autres méthodes
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
        #endregion
    }
}