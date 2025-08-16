using CommunityToolkit.Mvvm.Input;
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

namespace TransitManager.WPF.ViewModels
{
    public class VehiculeDetailViewModel : BaseViewModel
    {
        private readonly IVehiculeService _vehiculeService;
        private readonly IClientService _clientService;
        private readonly IConteneurService _conteneurService; // <-- NOUVEAU : Service injecté
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

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
						Vehicule.ConteneurId = (value?.Id == Guid.Empty) ? null : value?.Id;
					}
				}
			}
		}
        
        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand<System.Windows.Point> AddDamagePointCommand { get; }
        public IRelayCommand<System.Windows.Point> RemoveDamagePointCommand { get; }

        public VehiculeDetailViewModel(IVehiculeService vehiculeService, IClientService clientService, 
                                       IConteneurService conteneurService, // <-- NOUVEAU : Service injecté
                                       INavigationService navigationService, IDialogService dialogService)
        {
            _vehiculeService = vehiculeService;
            _clientService = clientService;
            _conteneurService = conteneurService; // <-- NOUVEAU : Service assigné
            _navigationService = navigationService;
            _dialogService = dialogService;

            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            AddDamagePointCommand = new RelayCommand<System.Windows.Point>(AddDamagePoint);
            RemoveDamagePointCommand = new RelayCommand<System.Windows.Point>(RemoveDamagePoint);
            
            DamagePoints.CollectionChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
            Rayures.StrokesChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
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
                });
            }
        }
        
        public async Task InitializeAsync(Guid vehiculeId)
        {
            await ExecuteBusyActionAsync(async () =>
            {
                _allClients = (await _clientService.GetActiveClientsAsync()).ToList();
                FilterClients(null);
                
                Title = "Modifier le Véhicule";
                Vehicule = await _vehiculeService.GetByIdAsync(vehiculeId);
                if (Vehicule != null)
                {
                    SelectedClient = Clients.FirstOrDefault(c => c.Id == Vehicule.ClientId);
                    LoadDamagePoints();
                    LoadRayures();
                    UpdatePlanImage(Vehicule.Type);

                    // --- NOUVELLE LOGIQUE D'INITIALISATION ---
                    await LoadConteneursDisponiblesAsync();
                    if (Vehicule.ConteneurId.HasValue)
                    {
                        SelectedConteneur = ConteneursDisponibles.FirstOrDefault(c => c.Id == Vehicule.ConteneurId.Value);
                    }
                    else
                    {
                        SelectedConteneur = ConteneursDisponibles.FirstOrDefault(c => c.Id == Guid.Empty);
                    }
                }
            });
        }

        // --- NOUVELLE MÉTHODE ---
        private async Task LoadConteneursDisponiblesAsync()
        {
            var conteneurs = await _conteneurService.GetOpenConteneursAsync();
            ConteneursDisponibles.Clear();
            ConteneursDisponibles.Add(new Conteneur { Id = Guid.Empty, NumeroDossier = "Aucun" }); 
            foreach (var c in conteneurs)
            {
                ConteneursDisponibles.Add(c);
            }
        }
        
		private async Task SaveAsync()
		{
			if (!CanSave() || Vehicule == null || SelectedClient == null) return;
			
			// --- NOUVELLE LOGIQUE DE STATUT ---
			if (Vehicule.ConteneurId.HasValue && Vehicule.Statut == StatutVehicule.EnAttente)
			{
				Vehicule.Statut = StatutVehicule.Affecte;
			}
			else if (!Vehicule.ConteneurId.HasValue && Vehicule.Statut != StatutVehicule.EnAttente)
			{
				// On ne remet EnAttente que si ce n'est pas un statut "problème"
				if(Vehicule.Statut != StatutVehicule.Probleme && Vehicule.Statut != StatutVehicule.Retourne)
				{
					Vehicule.Statut = StatutVehicule.EnAttente;
				}
			}
					
			Vehicule.ClientId = SelectedClient.Id;
			SerializeDamagePoints();
			SerializeRayures();

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
					_navigationService.GoBack(); // <-- On ferme la fenêtre après l'enregistrement
				}
				catch (Exception ex)
				{
					await _dialogService.ShowErrorAsync("Erreur", $"Erreur d'enregistrement : {ex.Message}\n{ex.InnerException?.Message}");
				}
			});
		}
        // Le reste du fichier est inchangé
        #region Reste du code (inchangé)
        private bool CanSave()
        {
            return Vehicule != null && SelectedClient != null &&
                   !string.IsNullOrWhiteSpace(Vehicule.Immatriculation) &&
                   !string.IsNullOrWhiteSpace(Vehicule.Marque) &&
                   !string.IsNullOrWhiteSpace(Vehicule.Modele) &&
                   !IsBusy;
        }
        private void Cancel() => _navigationService.GoBack();
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