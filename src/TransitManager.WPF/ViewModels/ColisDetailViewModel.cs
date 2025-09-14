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
using Microsoft.Extensions.DependencyInjection;
using TransitManager.Core.Exceptions;

namespace TransitManager.WPF.ViewModels
{
    public class ColisDetailViewModel : BaseViewModel, IRecipient<EntityTotalPaidUpdatedMessage>
    {
        private readonly IColisService _colisService;
        private readonly IClientService _clientService;
        private readonly IBarcodeService _barcodeService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IConteneurService _conteneurService;
		private readonly IPaiementService _paiementService;
		private readonly IServiceProvider _serviceProvider;
		private readonly IMessenger _messenger;
		private readonly IExportService _exportService;
		public Action? CloseAction { get; set; }
		private bool _isModal = false;

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
		private decimal _prixMetreCube;
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
		
		public decimal PrixMetreCube
		{
			get => _prixMetreCube;
			set
			{
				if (SetProperty(ref _prixMetreCube, value))
				{
					OnPropertyChanged(nameof(PrixEstime)); // Notifier que le prix estimé doit être recalculé
				}
			}
		}

		public decimal PrixEstime => (Colis?.Volume ?? 0) * PrixMetreCube;
		
		private void OnColisPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Colis.InventaireJson))
			{
				OnPropertyChanged(nameof(HasInventaire));
			}

			// AJOUTER CETTE CONDITION
			if (e.PropertyName == nameof(Colis.Volume))
			{
				OnPropertyChanged(nameof(PrixEstime));
			}
			
			// On notifie juste que quelque chose a changé pour que le bouton Enregistrer s'active/désactive
			SaveCommand.NotifyCanExecuteChanged();
		}

		private Conteneur? _selectedConteneur;
		public Conteneur? SelectedConteneur
		{
			get => _selectedConteneur;
			set
			{
				if (SetProperty(ref _selectedConteneur, value))
				{
					if (Colis != null)
					{
						// On met simplement à jour l'ID. L'utilisateur enregistrera pour valider.
						Colis.ConteneurId = (value?.Id == Guid.Empty) ? null : value?.Id;
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
		
        public void SetModalMode()
        {
            _isModal = true;
        }
		
		public ObservableCollection<StatutColis> AvailableStatuses { get; } = new();
		public bool HasInventaire => Colis != null && !string.IsNullOrEmpty(Colis.InventaireJson) && Colis.InventaireJson != "[]";
		
		public bool HasPaiements => Colis != null && Colis.Paiements.Any();


        #region Commandes
		public IAsyncRelayCommand CheckInventaireModificationCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand AddBarcodeCommand { get; }
        public IRelayCommand<Barcode> RemoveBarcodeCommand { get; }
        public IRelayCommand GenerateBarcodeCommand { get; }
		public IAsyncRelayCommand OpenInventaireCommand { get; }
		public IAsyncRelayCommand OpenPaiementCommand { get; }
		public IAsyncRelayCommand CheckPaiementModificationCommand { get; }
		public IAsyncRelayCommand OpenPrintPreviewCommand { get; }
        #endregion

        public ColisDetailViewModel(
            IColisService colisService, IClientService clientService, IBarcodeService barcodeService, 
            INavigationService navigationService, IDialogService dialogService, IConteneurService conteneurService, 
            IPaiementService paiementService, IServiceProvider serviceProvider, IMessenger messenger,
            IExportService exportService // AJOUTER CE PARAMÈTRE
        )
		{
			_colisService = colisService;
			_clientService = clientService;
			_barcodeService = barcodeService;
			_navigationService = navigationService;
			_dialogService = dialogService;
			_conteneurService = conteneurService;
			_paiementService = paiementService; 
			_serviceProvider = serviceProvider;
			_messenger = messenger;
            _exportService = exportService; // AJOUTER CETTE LIGNE D'ASSIGNATION
			
			_messenger.RegisterAll(this);
			_clientService.ClientStatisticsUpdated += OnDataShouldRefresh;

            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            AddBarcodeCommand = new RelayCommand(AddBarcode, () => !string.IsNullOrWhiteSpace(NewBarcode));
            RemoveBarcodeCommand = new RelayCommand<Barcode>(RemoveBarcode);
			OpenInventaireCommand = new AsyncRelayCommand(OpenInventaire);
			OpenPaiementCommand = new AsyncRelayCommand(OpenPaiement);
			CheckPaiementModificationCommand = new AsyncRelayCommand(CheckPaiementModification);
			CheckInventaireModificationCommand = new AsyncRelayCommand(CheckInventaireModification);
            GenerateBarcodeCommand = new RelayCommand(GenerateBarcode);
			OpenPrintPreviewCommand = new AsyncRelayCommand(OpenPrintPreviewAsync, CanPrint);
        }
		
        // AJOUTER CETTE MÉTHODE pour activer/désactiver le bouton
        private bool CanPrint()
        {
            // On peut imprimer seulement si le colis n'est pas nouveau (il a déjà été sauvegardé au moins une fois)
            return Colis != null && !string.IsNullOrEmpty(Colis.CreePar);
        }
        
        // AJOUTER CETTE NOUVELLE MÉTHODE
        private async Task OpenPrintPreviewAsync()
        {
            if (Colis == null) return;

            await ExecuteBusyActionAsync(async () =>
            {
                try
                {
                    // 1. Générer le PDF en mémoire (Cette ligne est maintenant correcte car _exportService existe)
                    var pdfData = await _exportService.GenerateColisTicketPdfAsync(Colis);

                    // 2. Créer la vue et le ViewModel de l'aperçu
                    using var scope = _serviceProvider.CreateScope();
                    var previewViewModel = scope.ServiceProvider.GetRequiredService<PrintPreviewViewModel>();
                    
                    // 3. Charger le PDF dans le ViewModel et initialiser
                    previewViewModel.LoadPdf(pdfData);
                    await previewViewModel.InitializeAsync();

                    // 4. Ouvrir la fenêtre modale
                    var previewWindow = new Views.PrintPreviewView(previewViewModel)
                    {
                        // CORRECTION POUR L'AMBIGUÏTÉ
                        Owner = System.Windows.Application.Current.MainWindow
                    };

                    previewWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur de génération", $"Impossible de générer l'étiquette : {ex.Message}");
                }
            });
        }		
		
		public void Receive(EntityTotalPaidUpdatedMessage message)
		{
			// On vérifie si le message concerne bien le colis actuellement affiché.
			if (Colis != null && Colis.Id == message.EntityId)
			{
				// On met à jour la propriété SommePayee, ce qui mettra à jour l'UI en temps réel.
				Colis.SommePayee = message.NewTotalPaid;
			}
		}
		
		private async Task OpenPaiement()
		{
			if (Colis == null) return;

			using var scope = _serviceProvider.CreateScope();
			var paiementViewModel = scope.ServiceProvider.GetRequiredService<PaiementColisViewModel>();
			
			// On initialise le ViewModel avec l'objet Colis complet
			await paiementViewModel.InitializeAsync(Colis);

			var paiementWindow = new Views.Paiements.PaiementColisView(paiementViewModel)
			{
				Owner = System.Windows.Application.Current.MainWindow
			};

			if (paiementWindow.ShowDialog() == true)
			{
				// À la fermeture, on récupère la somme calculée par le ViewModel de la fenêtre
				Colis.SommePayee = paiementViewModel.TotalValeur;
				
				// On notifie l'interface pour mettre à jour les champs liés
				OnPropertyChanged(nameof(HasPaiements));
			}
		}

		private async Task CheckPaiementModification()
		{
			if (HasPaiements)
			{
				var confirm = await _dialogService.ShowConfirmationAsync(
					"Paiements existants",
					"Le détail des paiements a déjà été commencé pour ce colis.\nVoulez-vous ouvrir la fenêtre de gestion des paiements ?");

				if (confirm)
				{
					await OpenPaiement();
				}
			}
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
			
			Colis.ClientId = SelectedClient.Id;
			Colis.Barcodes = Barcodes.ToList();

			// *** LOGIQUE MÉTIER AJOUTÉE ICI ***
			// C'est AU MOMENT D'ENREGISTRER qu'on applique les règles.
			var finalStatuses = new[] { StatutColis.Livre, StatutColis.Perdu, StatutColis.Probleme, StatutColis.Retourne };
			if (!finalStatuses.Contains(Colis.Statut))
			{
				Colis.Statut = Colis.ConteneurId.HasValue ? StatutColis.Affecte : StatutColis.EnAttente;
			}
			// Cas particulier du statut "Retourne"
			if(Colis.Statut == StatutColis.Retourne)
			{
				Colis.ConteneurId = null;
			}
			// *** FIN DE LA LOGIQUE MÉTIER ***

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

					// ==== MODIFICATION ICI ====
					if (_isModal)
					{
						// On ne ferme plus automatiquement, sauf si on décide de garder ce comportement pour les modales
						// CloseAction?.Invoke(); 
					}
					else
					{
						// On ne revient plus en arrière
						// _navigationService.GoBack(); 
					}

					// On recharge les données pour avoir l'état le plus récent après sauvegarde
					await InitializeAsync(Colis.Id);
					OpenPrintPreviewCommand.NotifyCanExecuteChanged();
					// ==== FIN DE LA MODIFICATION ====
				}
				catch (ConcurrencyException cex)
				{
					var refresh = await _dialogService.ShowConfirmationAsync(
						"Conflit de Données",
						$"{cex.Message}\n\nVoulez-vous rafraîchir les données pour voir les dernières modifications ? (Vos changements actuels seront perdus)");

					if (refresh && Colis != null)
					{
						await InitializeAsync(Colis.Id); // Recharge les données
					}
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
				var colisComplet = await _colisService.GetByIdAsync(colisId);
				if (colisComplet == null || colisComplet.Client == null)
				{
					await _dialogService.ShowErrorAsync("Erreur Critique", "Le colis ou son propriétaire est introuvable.");
					Cancel();
					return;
				}
				
				// Attacher l'écouteur d'événement AVANT d'assigner l'objet
				// pour garantir que les changements futurs sont interceptés.
				colisComplet.PropertyChanged += OnColisPropertyChanged;
				Colis = colisComplet;

				_allClients = (await _clientService.GetActiveClientsAsync()).ToList();
				if (!_allClients.Any(c => c.Id == Colis.ClientId))
				{
					 _allClients.Insert(0, Colis.Client);
				}
				Clients = new ObservableCollection<Client>(_allClients);
				SelectedClient = Clients.FirstOrDefault(c => c.Id == Colis.ClientId);

				Barcodes = new ObservableCollection<Barcode>(Colis.Barcodes);
				
				await LoadConteneursDisponiblesAsync();
				
				// La sélection du conteneur est simple
				if (Colis.ConteneurId.HasValue)
				{
					SelectedConteneur = ConteneursDisponibles.FirstOrDefault(c => c.Id == Colis.ConteneurId.Value);
				}
				else
				{
					SelectedConteneur = ConteneursDisponibles.FirstOrDefault(c => c.Id == Guid.Empty);
				}
				
				// On charge les statuts possibles UNE SEULE FOIS.
				LoadAvailableStatuses();
				
				if (SelectedClient != null && Colis.Destinataire == SelectedClient.NomComplet && Colis.TelephoneDestinataire == SelectedClient.TelephonePrincipal)
				{
					DestinataireEstProprietaire = true;
				}
				
				SaveCommand.NotifyCanExecuteChanged();
				OpenPrintPreviewCommand.NotifyCanExecuteChanged();
			});
		}


		private void LoadAvailableStatuses()
		{
			var currentStatus = Colis?.Statut; // Sauvegarder le statut
			AvailableStatuses.Clear();
			if (Colis == null) return;

			var statuses = new HashSet<StatutColis>
			{
				Colis.Statut,
				StatutColis.EnAttente,
				StatutColis.Affecte,
				StatutColis.Probleme,
				StatutColis.Perdu,
				StatutColis.Retourne,
				StatutColis.Livre
			};

			if (Colis.ConteneurId.HasValue && SelectedConteneur != null)
			{
				statuses.Add(GetNormalStatusFromContainerDates(SelectedConteneur));
			}

			foreach (var s in statuses.OrderBy(s => s.ToString()))
			{
				AvailableStatuses.Add(s);
			}

			// Rétablir le statut sur l'objet au cas où Clear l'aurait affecté via le binding
			if (currentStatus.HasValue)
			{
				Colis.Statut = currentStatus.Value;
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
		
		// ##### MÉTHODE À AJOUTER DANS LA CLASSE #####
		private async void OnDataShouldRefresh(Guid clientId)
		{
			// On ne recharge la vue que si le message concerne bien le client du colis affiché.
			if (Colis != null && Colis.ClientId == clientId)
			{
				// On utilise InitializeAsync pour recharger proprement toutes les données du colis depuis la BDD.
				await InitializeAsync(Colis.Id);
			}
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
