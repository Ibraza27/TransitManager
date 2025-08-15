using System.IO;
using System.Windows.Ink;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
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
        
        // --- NOUVELLE PROPRIÉTÉ POUR L'IMAGE ---
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

        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand<System.Windows.Point> AddDamagePointCommand { get; }
        public IRelayCommand<System.Windows.Point> RemoveDamagePointCommand { get; }

		public VehiculeDetailViewModel(IVehiculeService vehiculeService, IClientService clientService, INavigationService navigationService, IDialogService dialogService)
		{
			_vehiculeService = vehiculeService;
			_clientService = clientService;
			_navigationService = navigationService;
			_dialogService = dialogService;

			SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
			CancelCommand = new RelayCommand(Cancel);
			AddDamagePointCommand = new RelayCommand<System.Windows.Point>(AddDamagePoint);
			RemoveDamagePointCommand = new RelayCommand<System.Windows.Point>(RemoveDamagePoint);

			// S'abonner aux changements des points d'impact (déjà présent)
			DamagePoints.CollectionChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
			
			// LIGNE AJOUTÉE : S'abonner aux changements des rayures (traits dessinés)
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
                    Vehicule = new Vehicule(); // L'abonnement se fait via le setter
                    DestinataireEstProprietaire = true;
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

					// LIGNE AJOUTÉE : Mettre à jour l'image au chargement initial
					UpdatePlanImage(Vehicule.Type);
				}
			});
		}
        
        private bool CanSave()
        {
            return Vehicule != null && SelectedClient != null &&
                   !string.IsNullOrWhiteSpace(Vehicule.Immatriculation) &&
                   !string.IsNullOrWhiteSpace(Vehicule.Marque) &&
                   !string.IsNullOrWhiteSpace(Vehicule.Modele) &&
                   !IsBusy;
        }

        private async Task SaveAsync()
        {
            if (!CanSave() || Vehicule == null || SelectedClient == null) return;
            
            Vehicule.ClientId = SelectedClient.Id;
            SerializeDamagePoints();
			SerializeRayures();

            await ExecuteBusyActionAsync(async () =>
            {
                try
                {
                    bool isNew = Vehicule.CreePar == null;
                    if (isNew)
                    {
                        await _vehiculeService.CreateAsync(Vehicule);
                    }
                    else
                    {
                        await _vehiculeService.UpdateAsync(Vehicule);
                    }
                    await _dialogService.ShowInformationAsync("Succès", "Le véhicule a été enregistré.");
                    _navigationService.GoBack();
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", $"Erreur d'enregistrement : {ex.Message}");
                }
            });
        }

        private void Cancel() => _navigationService.GoBack();
        
        private void AddDamagePoint(System.Windows.Point point) => DamagePoints.Add(point);
        private void RemoveDamagePoint(System.Windows.Point point) => DamagePoints.Remove(point);

        private void SerializeDamagePoints()
        {
            if (Vehicule != null)
            {
                Vehicule.EtatDesLieux = JsonSerializer.Serialize(DamagePoints);
            }
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
                    if (points != null)
                    {
                        foreach (var p in points) DamagePoints.Add(p);
                    }
                }
                catch { /* Ignorer les erreurs de désérialisation */ }
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
					var loadedStrokes = new StrokeCollection(memoryStream);
					Rayures.Add(loadedStrokes);
				}
				catch { /* Ignorer si les données sont corrompues */ }
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
                var filtered = _allClients.Where(c =>
                    c.NomComplet.ToLower().Contains(searchTextLower) ||
                    c.TelephonePrincipal.Contains(searchTextLower)
                );
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

        // MÉTHODE AJOUTÉE : C'est elle qui réactive le bouton
        private void OnVehiculePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();

            // Si le type de véhicule change, on met à jour l'image
            if (e.PropertyName == nameof(Vehicule.Type) && Vehicule != null)
            {
                UpdatePlanImage(Vehicule.Type);
            }
        }

        // NOUVELLE MÉTHODE
        private void UpdatePlanImage(TypeVehicule type)
        {
            PlanImagePath = type switch
            {
                TypeVehicule.Voiture => "/Resources/Images/voiture_plan.png",
                TypeVehicule.Moto => "/Resources/Images/moto_plan.png",
                TypeVehicule.Scooter => "/Resources/Images/moto_plan.png", // On réutilise la même
                TypeVehicule.Camion => "/Resources/Images/camion_plan.png",
                TypeVehicule.Bus => "/Resources/Images/camion_plan.png", // On réutilise
                TypeVehicule.Van => "/Resources/Images/camion_plan.png", // On réutilise
                _ => "/Resources/Images/vehicule_plan.png" // Image par défaut
            };
        }
    }
}