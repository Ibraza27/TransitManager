using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Ink;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.WPF.Messages;

namespace TransitManager.WPF.ViewModels
{
    public class VehiculeDetailViewModel : BaseViewModel, IRecipient<EntityTotalPaidUpdatedMessage>
    {
        private readonly IVehiculeService _vehiculeService;
        private readonly IClientService _clientService;
        private readonly IConteneurService _conteneurService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPaiementService _paiementService;
		private readonly IMessenger _messenger;
		public Action? CloseAction { get; set; }
		private bool _isModal = false;
		
		public void SetModalMode()
		{
			_isModal = true;
		}

        private Vehicule? _vehicule;
        public Vehicule? Vehicule
        {
            get => _vehicule;
            set
            {
                if (_vehicule != null) _vehicule.PropertyChanged -= OnVehiculePropertyChanged;
                SetProperty(ref _vehicule, value);
                if (_vehicule != null) _vehicule.PropertyChanged += OnVehiculePropertyChanged;
            }
        }
        
        private string _planImagePath = "/Resources/Images/vehicule_plan.png";
        public string PlanImagePath { get => _planImagePath; set => SetProperty(ref _planImagePath, value); }

        private ObservableCollection<Client> _clients = new();
        public ObservableCollection<Client> Clients { get => _clients; set => SetProperty(ref _clients, value); }
        
        private Client? _selectedClient;
        public Client? SelectedClient
        {
            get => _selectedClient;
            set
            {
                if (SetProperty(ref _selectedClient, value))
                {
                    if (DestinataireEstProprietaire) { UpdateDestinataire(); }
                    SaveCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private List<Client> _allClients = new();
        private string? _clientSearchText;
        public string? ClientSearchText
        {
            get => _clientSearchText;
            set
            {
                if (SetProperty(ref _clientSearchText, value) && value != SelectedClient?.NomComplet)
                {
                    FilterClients(value);
                }
            }
        }

        private bool _destinataireEstProprietaire;
        public bool DestinataireEstProprietaire
        {
            get => _destinataireEstProprietaire;
            set
            {
                if (SetProperty(ref _destinataireEstProprietaire, value))
                {
                    UpdateDestinataire();
                }
            }
        }

        public ObservableCollection<System.Windows.Point> DamagePoints { get; set; } = new();
        public StrokeCollection Rayures { get; set; } = new StrokeCollection();

        // --- NOUVELLES PROPRIÉTÉS POUR L'AFFECTATION ---
        public ObservableCollection<Conteneur> ConteneursDisponibles { get; } = new();

		
		public ObservableCollection<StatutVehicule> AvailableStatuses { get; } = new();
		
		private Conteneur? _selectedConteneur;
		public Conteneur? SelectedConteneur
		{
			get => _selectedConteneur;
			set 
			{
				if (SetProperty(ref _selectedConteneur, value))
				{
					if (Vehicule != null)
					{
						// On met simplement à jour l'ID. La logique métier sera appliquée à l'enregistrement.
						Vehicule.ConteneurId = (value?.Id == Guid.Empty) ? null : value?.Id;
					}
				}
			}
		}
        
        public bool HasPaiements => Vehicule != null && Vehicule.Paiements.Any();

        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand<System.Windows.Point> AddDamagePointCommand { get; }
        public IRelayCommand<System.Windows.Point> RemoveDamagePointCommand { get; }
        public IAsyncRelayCommand OpenPaiementCommand { get; }
        public IAsyncRelayCommand CheckPaiementModificationCommand { get; }

        public VehiculeDetailViewModel(IVehiculeService vehiculeService, IClientService clientService, 
                                       IConteneurService conteneurService, INavigationService navigationService, 
                                       IDialogService dialogService, IServiceProvider serviceProvider, IPaiementService paiementService, IMessenger messenger)
        {
            _vehiculeService = vehiculeService;
            _clientService = clientService;
            _conteneurService = conteneurService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _serviceProvider = serviceProvider;
            _paiementService = paiementService;
			_messenger = messenger;
			_messenger.RegisterAll(this);

            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            AddDamagePointCommand = new RelayCommand<System.Windows.Point>(AddDamagePoint);
            RemoveDamagePointCommand = new RelayCommand<System.Windows.Point>(RemoveDamagePoint);
            OpenPaiementCommand = new AsyncRelayCommand(OpenPaiement);
            CheckPaiementModificationCommand = new AsyncRelayCommand(CheckPaiementModification);
            
            DamagePoints.CollectionChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
            Rayures.StrokesChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
        }
		
		public void Receive(EntityTotalPaidUpdatedMessage message)
		{
			if (Vehicule != null && Vehicule.Id == message.EntityId)
			{
				Vehicule.SommePayee = message.NewTotalPaid;
			}
		}


        private async Task OpenPaiement()
        {
            if (Vehicule == null) return;

            using var scope = _serviceProvider.CreateScope();
            var paiementViewModel = scope.ServiceProvider.GetRequiredService<PaiementVehiculeViewModel>();
            await paiementViewModel.InitializeAsync(Vehicule);

            var paiementWindow = new Views.Paiements.PaiementVehiculeView(paiementViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (paiementWindow.ShowDialog() == true)
            {
                var updatedVehicule = await _vehiculeService.GetByIdAsync(Vehicule.Id);
                if(updatedVehicule != null)
                {
                    Vehicule.SommePayee = updatedVehicule.Paiements.Sum(p => p.Montant);
                }
                OnPropertyChanged(nameof(HasPaiements));
            }
        }

        private async Task CheckPaiementModification()
        {
            if (HasPaiements)
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Paiements existants",
                    "Le détail des paiements a déjà été commencé pour ce véhicule.\nVoulez-vous ouvrir la fenêtre de gestion des paiements ?");

                if (confirm)
                {
                    await OpenPaiement();
                }
            }
        }
        
        public async Task InitializeAsync(string newMarker)
        {
            if (newMarker == "new")
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    _allClients = (await _clientService.GetActiveClientsAsync()).ToList();
                    FilterClients(null);
                    Title = "Nouveau Véhicule";
                    Vehicule = new Vehicule();
                    DestinataireEstProprietaire = true;
                    
                    await LoadConteneursDisponiblesAsync(); // <-- NOUVEAU
					LoadAvailableStatuses();
                });
            }
        }
        
		public async Task InitializeAsync(Guid vehiculeId)
		{
			await ExecuteBusyActionAsync(async () =>
			{
				Title = "Modifier le Véhicule";
				
				var vehiculeComplet = await _vehiculeService.GetByIdAsync(vehiculeId);
				if (vehiculeComplet == null || vehiculeComplet.Client == null)
				{
					await _dialogService.ShowErrorAsync("Erreur", "Le véhicule ou son propriétaire est introuvable.");
					Cancel();
					return;
				}
				
				// Attacher l'écouteur d'événement
				vehiculeComplet.PropertyChanged += OnVehiculePropertyChanged;
				Vehicule = vehiculeComplet;

				_allClients = (await _clientService.GetActiveClientsAsync()).ToList();
				if (!_allClients.Any(c => c.Id == Vehicule.ClientId))
				{
					_allClients.Insert(0, Vehicule.Client);
				}
				Clients = new ObservableCollection<Client>(_allClients);
				
				SelectedClient = Clients.FirstOrDefault(c => c.Id == Vehicule.ClientId);

				LoadDamagePoints();
				LoadRayures();
				UpdatePlanImage(Vehicule.Type);

				await LoadConteneursDisponiblesAsync();
				
				if (Vehicule.ConteneurId.HasValue)
				{
					SelectedConteneur = ConteneursDisponibles.FirstOrDefault(c => c.Id == Vehicule.ConteneurId.Value);
				}
				else
				{
					SelectedConteneur = ConteneursDisponibles.FirstOrDefault(c => c.Id == Guid.Empty);
				}
				
				LoadAvailableStatuses();

				if (SelectedClient != null && Vehicule.Destinataire == SelectedClient.NomComplet && Vehicule.TelephoneDestinataire == SelectedClient.TelephonePrincipal)
				{
					DestinataireEstProprietaire = true;
				}
			});
		}
		
        private void LoadAvailableStatuses()
        {
            AvailableStatuses.Clear();
            if (Vehicule == null) return;

            var statuses = new HashSet<StatutVehicule>();

            // 1. Ajouter le statut actuel du véhicule
            statuses.Add(Vehicule.Statut);

            // 2. Ajouter les statuts manuels importants
            statuses.Add(StatutVehicule.Probleme);
            statuses.Add(StatutVehicule.Retourne);
            statuses.Add(StatutVehicule.Livre);

            // 3. Ajouter le statut "normal" basé sur les DATES du conteneur
            if (SelectedConteneur != null && SelectedConteneur.Id != Guid.Empty)
            {
                var containerDrivenStatus = GetNormalStatusFromContainerDates(SelectedConteneur);
                statuses.Add(containerDrivenStatus);
            }
            else
            {
                statuses.Add(StatutVehicule.EnAttente);
            }

            // Remplir la liste triée
            foreach (var s in statuses.OrderBy(s => s.ToString()))
            {
                AvailableStatuses.Add(s);
            }
        }	

        private StatutVehicule GetNormalStatusFromContainerDates(Conteneur conteneur)
        {
            if (conteneur.DateCloture.HasValue) return StatutVehicule.Livre;
            if (conteneur.DateDedouanement.HasValue) return StatutVehicule.EnDedouanement;
            if (conteneur.DateArriveeDestination.HasValue) return StatutVehicule.Arrive;
            if (conteneur.DateDepart.HasValue) return StatutVehicule.EnTransit;

            return StatutVehicule.Affecte;
        }

        // --- NOUVELLE MÉTHODE ---
        private async Task LoadConteneursDisponiblesAsync()
        {
            // On charge tous les conteneurs qui peuvent recevoir des véhicules
            var conteneurs = (await _conteneurService.GetAllAsync())
                .Where(c => c.Statut == StatutConteneur.Reçu || 
                            c.Statut == StatutConteneur.EnPreparation ||
                            c.Statut == StatutConteneur.Probleme)
                .ToList();

            ConteneursDisponibles.Clear();
            ConteneursDisponibles.Add(new Conteneur { Id = Guid.Empty, NumeroDossier = "Aucun" });

            // S'assurer que le conteneur actuel du véhicule est dans la liste, même si son statut a changé
            if (Vehicule?.ConteneurId.HasValue == true && !conteneurs.Any(c => c.Id == Vehicule.ConteneurId.Value))
            {
                var currentConteneur = await _conteneurService.GetByIdAsync(Vehicule.ConteneurId.Value);
                if (currentConteneur != null)
                {
                    conteneurs.Add(currentConteneur);
                }
            }
            
            foreach (var c in conteneurs.OrderBy(c => c.NumeroDossier))
            {
                ConteneursDisponibles.Add(c);
            }
        }
        
		private async Task SaveAsync()
		{
			if (!CanSave() || Vehicule == null || SelectedClient == null) return;
					
			Vehicule.ClientId = SelectedClient.Id;
			SerializeDamagePoints();
			SerializeRayures();

			// *** LOGIQUE MÉTIER AJOUTÉE ICI, AVANT LA SAUVEGARDE ***
			var finalStatuses = new[] { StatutVehicule.Livre, StatutVehicule.Probleme, StatutVehicule.Retourne };
			
			// On n'applique la logique automatique que si le statut n'est pas un statut final choisi par l'utilisateur.
			if (!finalStatuses.Contains(Vehicule.Statut))
			{
				Vehicule.Statut = Vehicule.ConteneurId.HasValue ? StatutVehicule.Affecte : StatutVehicule.EnAttente;
			}

			// Cas particulier : si le statut est "Retourne", on s'assure que le conteneur est bien retiré.
			if(Vehicule.Statut == StatutVehicule.Retourne)
			{
				Vehicule.ConteneurId = null;
			}
			// *** FIN DE LA LOGIQUE MÉTIER ***

			await ExecuteBusyActionAsync(async () =>
			{
				try
				{
					bool isNew = string.IsNullOrEmpty(Vehicule.CreePar);
					if (isNew)
					{
						await _vehiculeService.CreateAsync(Vehicule);
					}
					else
					{
						await _vehiculeService.UpdateAsync(Vehicule);
					}

					await _dialogService.ShowInformationAsync("Succès", "Le véhicule a été enregistré.");
					
					if (_isModal)
					{
						CloseAction?.Invoke();
					}
					else
					{
						_navigationService.GoBack();
					}
				}
				catch (Exception ex)
				{
					await _dialogService.ShowErrorAsync("Erreur", $"Erreur d'enregistrement : {ex.Message}\n{ex.InnerException?.Message}");
				}
			});
		}


        #region Reste du code (inchangé)
        private bool CanSave()
        {
            return Vehicule != null && SelectedClient != null &&
                   !string.IsNullOrWhiteSpace(Vehicule.Immatriculation) &&
                   !string.IsNullOrWhiteSpace(Vehicule.Marque) &&
                   !string.IsNullOrWhiteSpace(Vehicule.Modele) &&
                   !IsBusy;
        }
		private void Cancel()
		{
			if (_isModal)
			{
				CloseAction?.Invoke();
			}
			else
			{
				_navigationService.GoBack();
			}
		}
        private void AddDamagePoint(System.Windows.Point point) => DamagePoints.Add(point);
        private void RemoveDamagePoint(System.Windows.Point point) => DamagePoints.Remove(point);
        private void SerializeDamagePoints()
        {
            if (Vehicule != null) Vehicule.EtatDesLieux = JsonSerializer.Serialize(DamagePoints);
        }
        private void SerializeRayures()
        {
            if (Vehicule == null) return;
            using var memoryStream = new MemoryStream();
            Rayures.Save(memoryStream);
            Vehicule.EtatDesLieuxRayures = Convert.ToBase64String(memoryStream.ToArray());
        }
        private void LoadDamagePoints()
        {
            DamagePoints.Clear();
            if (Vehicule != null && !string.IsNullOrEmpty(Vehicule.EtatDesLieux))
            {
                try
                {
                    var points = JsonSerializer.Deserialize<List<System.Windows.Point>>(Vehicule.EtatDesLieux);
                    if (points != null) foreach (var p in points) DamagePoints.Add(p);
                } catch { /* Ignorer */ }
            }
        }
        private void LoadRayures()
        {
            Rayures.Clear();
            if (Vehicule != null && !string.IsNullOrEmpty(Vehicule.EtatDesLieuxRayures))
            {
                try
                {
                    byte[] bytes = Convert.FromBase64String(Vehicule.EtatDesLieuxRayures);
                    using var memoryStream = new MemoryStream(bytes);
                    Rayures.Add(new StrokeCollection(memoryStream));
                } catch { /* Ignorer */ }
            }
        }
        private void FilterClients(string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Clients = new ObservableCollection<Client>(_allClients);
            }
            else
            {
                var searchTextLower = searchText.ToLower();
                var filtered = _allClients.Where(c => c.NomComplet.ToLower().Contains(searchTextLower) || c.TelephonePrincipal.Contains(searchTextLower));
                Clients = new ObservableCollection<Client>(filtered);
            }
        }
        private void UpdateDestinataire()
        {
            if (Vehicule == null) return;
            if (DestinataireEstProprietaire && SelectedClient != null)
            {
                Vehicule.Destinataire = SelectedClient.NomComplet;
                Vehicule.TelephoneDestinataire = SelectedClient.TelephonePrincipal;
            }
        }
		
		private void OnVehiculePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			SaveCommand.NotifyCanExecuteChanged();
			if (e.PropertyName == nameof(Vehicule.Type) && Vehicule != null)
			{
				UpdatePlanImage(Vehicule.Type);
			}
		}
		
        private void UpdatePlanImage(TypeVehicule type)
        {
            PlanImagePath = type switch
            {
                TypeVehicule.Voiture => "/Resources/Images/voiture_plan.png",
                TypeVehicule.Moto => "/Resources/Images/moto_plan.png",
                TypeVehicule.Scooter => "/Resources/Images/moto_plan.png",
                TypeVehicule.Camion => "/Resources/Images/camion_plan.png",
                TypeVehicule.Bus => "/Resources/Images/camion_plan.png",
                TypeVehicule.Van => "/Resources/Images/camion_plan.png",
                _ => "/Resources/Images/vehicule_plan.png"
            };
        }
        #endregion
    }
}