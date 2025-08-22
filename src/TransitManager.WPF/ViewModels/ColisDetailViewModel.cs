using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using System.Text.Json;
using TransitManager.WPF.Views.Inventaire;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.WPF.Messages;
using System.Windows; // Directive using ajoutée
using System.Windows.Input;
using System.ComponentModel;

namespace TransitManager.WPF.ViewModels
{
    public class ColisDetailViewModel : BaseViewModel
    {
        private readonly IColisService _colisService;
        private readonly IClientService _clientService;
        private readonly IBarcodeService _barcodeService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IConteneurService _conteneurService;

        #region Champs Privés
        private Colis? _colis;
        private ObservableCollection<Client> _clients = new();
        private List<Client> _allClients = new();
        private ObservableCollection<Barcode> _barcodes = new();
        private ObservableCollection<Conteneur> _conteneursDisponibles = new();
        private string _newBarcode = string.Empty;
        private bool _destinataireEstProprietaire;
        private Client? _selectedClient;
        private string? _clientSearchText;
        #endregion

        #region Propriétés Publiques

		public Colis? Colis
		{
			get => _colis;
			set
			{
				if (_colis != null)
				{
					_colis.PropertyChanged -= OnColisPropertyChanged;
				}
				SetProperty(ref _colis, value);
				if (_colis != null)
				{
					_colis.PropertyChanged += OnColisPropertyChanged;
				}
				// Notifier le changement de HasInventaire
				OnPropertyChanged(nameof(HasInventaire));
			}
		}

        public ObservableCollection<Client> Clients
        {
            get => _clients;
            set => SetProperty(ref _clients, value);
        }

        public ObservableCollection<Barcode> Barcodes
        {
            get => _barcodes;
            set
            {
                if (_barcodes != null)
                {
                    _barcodes.CollectionChanged -= OnBarcodesCollectionChanged;
                }
                SetProperty(ref _barcodes, value);
                if (_barcodes != null)
                {
                    _barcodes.CollectionChanged += OnBarcodesCollectionChanged;
                }
            }
        }

        public ObservableCollection<Conteneur> ConteneursDisponibles
        {
            get => _conteneursDisponibles;
            set => SetProperty(ref _conteneursDisponibles, value);
        }

        public string NewBarcode
        {
            get => _newBarcode;
            set
            {
                if (SetProperty(ref _newBarcode, value))
                {
                    AddBarcodeCommand.NotifyCanExecuteChanged();
                }
            }
        }

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

        public Client? SelectedClient
        {
            get => _selectedClient;
            set
            {
                if (SetProperty(ref _selectedClient, value))
                {
                    SaveCommand.NotifyCanExecuteChanged();
                    if (DestinataireEstProprietaire)
                    {
                        UpdateDestinataire();
                    }
                }
            }
        }

		private Conteneur? _selectedConteneur;
		public Conteneur? SelectedConteneur
		{
			get => _selectedConteneur;
			set 
			{
				if (SetProperty(ref _selectedConteneur, value))
				{
					// On ne sauvegarde plus automatiquement.
					// On met simplement à jour l'ID et on laisse l'utilisateur cliquer sur "Enregistrer".
					if (Colis != null)
					{
						Colis.ConteneurId = (value?.Id == Guid.Empty) ? null : value?.Id;
						// Le PropertyChanged sur Colis déclenchera la réévaluation de CanSave
					}
				}
			}
		}

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
        #endregion
		
		public ObservableCollection<StatutColis> AvailableStatuses { get; } = new();
		public bool HasInventaire => Colis != null && !string.IsNullOrEmpty(Colis.InventaireJson) && Colis.InventaireJson != "[]";


        #region Commandes
		public IAsyncRelayCommand CheckInventaireModificationCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand AddBarcodeCommand { get; }
        public IRelayCommand<Barcode> RemoveBarcodeCommand { get; }
        public IRelayCommand GenerateBarcodeCommand { get; }
		public IAsyncRelayCommand OpenInventaireCommand { get; }
        #endregion

        public ColisDetailViewModel(IColisService colisService, IClientService clientService, IBarcodeService barcodeService, INavigationService navigationService, IDialogService dialogService, IConteneurService conteneurService)
        {
            _colisService = colisService;
            _clientService = clientService;
            _barcodeService = barcodeService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _conteneurService = conteneurService;

            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            AddBarcodeCommand = new RelayCommand(AddBarcode, () => !string.IsNullOrWhiteSpace(NewBarcode));
            RemoveBarcodeCommand = new RelayCommand<Barcode>(RemoveBarcode);
			OpenInventaireCommand = new AsyncRelayCommand(OpenInventaire);
			CheckInventaireModificationCommand = new AsyncRelayCommand(CheckInventaireModification);
            GenerateBarcodeCommand = new RelayCommand(GenerateBarcode);
        }
		
        private async Task CheckInventaireModification()
        {
            if (HasInventaire) // Cette condition empêche le dialogue de s'afficher si l'inventaire est vide
            {
                var confirm = await _dialogService.ShowConfirmationAsync(
                    "Modification Manuelle",
                    "Les valeurs de ce champ sont calculées depuis l'inventaire.\nVoulez-vous ouvrir l'inventaire pour faire vos modifications ?");

                if (confirm)
                {
                    await OpenInventaire();
                }
            }
        }

        private bool CanSave()
        {
            return Colis != null &&
                   SelectedClient != null &&
                   Barcodes.Any() &&
                   !string.IsNullOrWhiteSpace(Colis.DestinationFinale) &&
                   !string.IsNullOrWhiteSpace(Colis.Destinataire) &&
                   Colis.NombrePieces > 0 &&
                   !IsBusy;
        }

        private async Task SaveAsync()
		{
			if (!CanSave() || Colis == null || SelectedClient == null) return;
            
            // La logique complexe est maintenant dans le service, le ViewModel envoie juste les données.
			Colis.ClientId = SelectedClient.Id;
			Colis.Barcodes = Barcodes.ToList();

			await ExecuteBusyActionAsync(async () =>
			{
				try
				{
					bool isNew = string.IsNullOrEmpty(Colis.CreePar);
					if (isNew)
					{
						await _colisService.CreateAsync(Colis);
					}
					else
					{
						await _colisService.UpdateAsync(Colis);
					}
					await _dialogService.ShowInformationAsync("Succès", "Le colis a été enregistré.");
					_navigationService.GoBack();
				}
				catch (Exception ex)
				{
					await _dialogService.ShowErrorAsync("Erreur", $"Erreur d'enregistrement : {ex.Message}\n{ex.InnerException?.Message}");
				}
			});
		}

        private void OnBarcodesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();
        }

        private void Cancel() => _navigationService.GoBack();

        private void AddBarcode()
        {
            if (!string.IsNullOrWhiteSpace(NewBarcode))
            {
                Barcodes.Add(new Barcode { Value = NewBarcode });
                NewBarcode = string.Empty;
            }
        }

        private void RemoveBarcode(Barcode? barcode)
        {
            if (barcode != null) Barcodes.Remove(barcode);
        }

        private void GenerateBarcode()
        {
            Barcodes.Add(new Barcode { Value = _barcodeService.GenerateBarcode() });
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
            if (Colis == null) return;
            if (DestinataireEstProprietaire && SelectedClient != null)
            {
                Colis.Destinataire = SelectedClient.NomComplet;
                Colis.TelephoneDestinataire = SelectedClient.TelephonePrincipal;
            }
        }


		private void OnColisPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			SaveCommand.NotifyCanExecuteChanged();
			
			// Notifier HasInventaire quand InventaireJson change
			if (e.PropertyName == nameof(Colis.InventaireJson))
			{
				OnPropertyChanged(nameof(HasInventaire));
			}
		}

        private void CalculatePrice()
        {
            if (Colis == null) return;
            decimal prix = 0;
            prix += Colis.Poids * 2.5m;
            if (Colis.TypeEnvoi == TypeEnvoi.AvecDedouanement) prix += 50m; else prix += 10m;
            if (Colis.LivraisonADomicile) prix += 15m;
            Colis.PrixTotal = prix;
        }

        public async Task InitializeAsync(string newMarker)
        {
            if (newMarker == "new")
            {
                Title = "Nouveau Colis";
                _allClients = (await _clientService.GetActiveClientsAsync()).ToList();
                FilterClients(null);
                Colis = new Colis();
                Barcodes = new ObservableCollection<Barcode>();
                Colis.PropertyChanged += OnColisPropertyChanged;
                DestinataireEstProprietaire = true;
                await LoadConteneursDisponiblesAsync();
				
				LoadAvailableStatuses();
				
            }
        }

		private async Task OpenInventaire()
		{
			if (Colis == null) return;
			var inventaireViewModel = new InventaireViewModel(Colis.InventaireJson);
			var inventaireWindow = new InventaireView(inventaireViewModel)
			{
				Owner = System.Windows.Application.Current.MainWindow
			};

			if (inventaireWindow.ShowDialog() == true)
			{
				Colis.InventaireJson = JsonSerializer.Serialize(inventaireViewModel.Items);
				Colis.NombrePieces = inventaireViewModel.TotalQuantite;
				Colis.ValeurDeclaree = inventaireViewModel.TotalValeur;
				
				// Notifier explicitement le changement de HasInventaire
				OnPropertyChanged(nameof(HasInventaire));
			}
		}

        public async Task InitializeAsync(Guid colisId)
        {
            Title = "Modifier le Colis";
            await ExecuteBusyActionAsync(async () =>
            {
                _allClients = (await _clientService.GetActiveClientsAsync()).ToList();
                FilterClients(null);
                Colis = await _colisService.GetByIdAsync(colisId);
                if (Colis != null)
                {
                    Barcodes = new ObservableCollection<Barcode>(Colis.Barcodes);
                    SelectedClient = Clients.FirstOrDefault(c => c.Id == Colis.ClientId);
                    Colis.PropertyChanged -= OnColisPropertyChanged;
					Colis.PropertyChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
                    await LoadConteneursDisponiblesAsync();
					OnPropertyChanged(nameof(HasInventaire));
					
                    if (Colis.ConteneurId.HasValue)
                    {
                        SelectedConteneur = ConteneursDisponibles.FirstOrDefault(c => c.Id == Colis.ConteneurId.Value);
                    }
                    LoadAvailableStatuses();
                    
                    // --- LA CORRECTION EST ICI ---
                    // On vérifie si les informations du destinataire correspondent à celles du propriétaire
                    if (SelectedClient != null && Colis.Destinataire == SelectedClient.NomComplet && Colis.TelephoneDestinataire == SelectedClient.TelephonePrincipal)
                    {
                        DestinataireEstProprietaire = true;
                    }
                }
            });
        }
		
        private void LoadAvailableStatuses()
        {
            AvailableStatuses.Clear();
            if (Colis == null) return;

            var statuses = new HashSet<StatutColis>();

            // 1. Ajouter le statut actuel du colis
            statuses.Add(Colis.Statut);
            
            // 2. Ajouter les statuts manuels importants
            statuses.Add(StatutColis.Probleme);
            statuses.Add(StatutColis.Perdu);
            statuses.Add(StatutColis.Retourne);
            statuses.Add(StatutColis.Livre);

            // 3. Ajouter le statut "normal" basé sur les DATES du conteneur (et non plus son statut)
            if (SelectedConteneur != null && SelectedConteneur.Id != Guid.Empty)
            {
                var containerDrivenStatus = GetNormalStatusFromContainerDates(SelectedConteneur);
                statuses.Add(containerDrivenStatus);
            }
            else
            {
                // Si pas de conteneur, le statut normal est "EnAttente"
                statuses.Add(StatutColis.EnAttente);
            }

            // Remplir la liste triée pour l'affichage
            foreach (var s in statuses.OrderBy(s => s.ToString()))
            {
                AvailableStatuses.Add(s);
            }
        }
		
        private StatutColis GetNormalStatusFromContainerDates(Conteneur conteneur)
        {
            if (conteneur.DateCloture.HasValue) return StatutColis.Livre;
            if (conteneur.DateDedouanement.HasValue) return StatutColis.EnDedouanement;
            if (conteneur.DateArriveeDestination.HasValue) return StatutColis.Arrive;
            if (conteneur.DateDepart.HasValue) return StatutColis.EnTransit;
            
            // Si aucune des dates ci-dessus n'est remplie, le statut normal est "Affecté"
            return StatutColis.Affecte;
        }
		
        private async Task LoadConteneursDisponiblesAsync()
        {
            // On charge tous les conteneurs qui peuvent recevoir des colis
            var conteneurs = (await _conteneurService.GetAllAsync())
                .Where(c => c.Statut == StatutConteneur.Reçu || 
                            c.Statut == StatutConteneur.EnPreparation ||
                            c.Statut == StatutConteneur.Probleme)
                .ToList();

            ConteneursDisponibles.Clear();
            ConteneursDisponibles.Add(new Conteneur { Id = Guid.Empty, NumeroDossier = "Aucun" });

            // S'assurer que le conteneur actuel du colis est dans la liste, même si son statut a changé
            if (Colis?.ConteneurId.HasValue == true && !conteneurs.Any(c => c.Id == Colis.ConteneurId.Value))
            {
                var currentConteneur = await _conteneurService.GetByIdAsync(Colis.ConteneurId.Value);
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

        private async void HandleConteneurSelectionChange()
        {
            if (Colis == null || IsBusy) return;
            var newConteneurId = _selectedConteneur?.Id == Guid.Empty ? null : _selectedConteneur?.Id;

            if (Colis.ConteneurId == newConteneurId) return;
            if (newConteneurId.HasValue)
            {
                await _colisService.AssignToConteneurAsync(Colis.Id, newConteneurId.Value);
            }
            else
            {
                await _colisService.RemoveFromConteneurAsync(Colis.Id);
            }

            await InitializeAsync(Colis.Id);
        }
    }
}
