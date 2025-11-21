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
using TransitManager.Core.Messages;
using System.Windows; // Directive using ajoutée
using System.Windows.Input;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using TransitManager.Core.Exceptions;
using TransitManager.Core.DTOs;

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
		
		// Remplacez la propriété existante par celle-ci :
		public bool HasPaiements => Colis != null && (Colis.Paiements.Any() || Colis.SommePayee > 0);


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
			
			// On passe les données actuelles
			await paiementViewModel.InitializeAsync(Colis.Id, Colis.ClientId, Colis.PrixTotal);

			var paiementWindow = new Views.Paiements.PaiementColisView(paiementViewModel)
			{
				Owner = System.Windows.Application.Current.MainWindow
			};

			if (paiementWindow.ShowDialog() == true)
			{
				// 1. Mettre à jour le montant affiché immédiatement
				Colis.SommePayee = paiementViewModel.TotalValeur;

				// 2. On force la notification pour 'HasPaiements' pour dégriser les champs si nécessaire
				// Même si la liste .Paiements n'est pas rechargée, on sait qu'il y en a si le total > 0
				OnPropertyChanged(nameof(HasPaiements));
				
				// 3. On signale que le colis a changé pour activer le bouton "Enregistrer"
				// (Ceci est géré par le setter de SommePayee si vous avez mis SetProperty, sinon :)
				SaveCommand.NotifyCanExecuteChanged();
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
			// Note : la liste de barcodes du ViewModel est déjà à jour, pas besoin de la réassigner ici.

			// Logique métier pour le statut
			var finalStatuses = new[] { StatutColis.Livre, StatutColis.Perdu, StatutColis.Probleme, StatutColis.Retourne };
			if (!finalStatuses.Contains(Colis.Statut))
			{
				Colis.Statut = Colis.ConteneurId.HasValue ? StatutColis.Affecte : StatutColis.EnAttente;
			}
			if(Colis.Statut == StatutColis.Retourne)
			{
				Colis.ConteneurId = null;
			}

			await ExecuteBusyActionAsync(async () =>
			{
				try
				{
					bool isNew = string.IsNullOrEmpty(Colis.CreePar);

					if (isNew)
					{
						var createDto = new CreateColisDto
						{
							ClientId = SelectedClient.Id,
							Designation = Colis.Designation,
							DestinationFinale = Colis.DestinationFinale,
							Barcodes = Barcodes.Select(b => b.Value).ToList(),
							NombrePieces = Colis.NombrePieces,
							Volume = Colis.Volume,
							ValeurDeclaree = Colis.ValeurDeclaree,
							PrixTotal = Colis.PrixTotal,
							Destinataire = Colis.Destinataire,
							TelephoneDestinataire = Colis.TelephoneDestinataire,
							LivraisonADomicile = Colis.LivraisonADomicile,
							AdresseLivraison = Colis.AdresseLivraison,
							EstFragile = Colis.EstFragile,
							ManipulationSpeciale = Colis.ManipulationSpeciale,
							InstructionsSpeciales = Colis.InstructionsSpeciales,
							Type = Colis.Type,
							TypeEnvoi = Colis.TypeEnvoi,
                            // On peut aussi passer le ConteneurId si sélectionné
                            ConteneurId = Colis.ConteneurId
						};
						var createdColis = await _colisService.CreateAsync(createDto);
                        Colis.Id = createdColis.Id; // Mettre à jour l'ID pour les actions futures
					}
					else
					{
						var updateDto = new UpdateColisDto
						{
							Id = Colis.Id,
							ClientId = SelectedClient.Id,
							Designation = Colis.Designation,
							DestinationFinale = Colis.DestinationFinale,
							Barcodes = Barcodes.Select(b => b.Value).ToList(),
							NombrePieces = Colis.NombrePieces,
							Volume = Colis.Volume,
							ValeurDeclaree = Colis.ValeurDeclaree,
							PrixTotal = Colis.PrixTotal,
							Destinataire = Colis.Destinataire,
							TelephoneDestinataire = Colis.TelephoneDestinataire,
							LivraisonADomicile = Colis.LivraisonADomicile,
							AdresseLivraison = Colis.AdresseLivraison,
							EstFragile = Colis.EstFragile,
							ManipulationSpeciale = Colis.ManipulationSpeciale,
							InstructionsSpeciales = Colis.InstructionsSpeciales,
							Type = Colis.Type,
							TypeEnvoi = Colis.TypeEnvoi,
                            ConteneurId = Colis.ConteneurId,
                            Statut = Colis.Statut
						};
						await _colisService.UpdateAsync(Colis.Id, updateDto);
					}

					await _dialogService.ShowInformationAsync("Succès", "Le colis a été enregistré.");
                    _messenger.Send(new ConteneurUpdatedMessage(true)); // Notifier que les listes doivent être rechargées

					if (_isModal)
					{
						// Optionnel : on peut choisir de fermer ou non
					}
					else
					{
						_navigationService.GoBack();
					}

					await InitializeAsync(Colis.Id);
					OpenPrintPreviewCommand.NotifyCanExecuteChanged();
				}
				catch (ConcurrencyException cex)
				{
					var refresh = await _dialogService.ShowConfirmationAsync(
						"Conflit de Données",
						$"{cex.Message}\n\nVoulez-vous rafraîchir les données pour voir les dernières modifications ? (Vos changements actuels seront perdus)");

					if (refresh && Colis != null)
					{
						await InitializeAsync(Colis.Id);
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
			
			// On passe le JSON actuel
			var inventaireViewModel = new InventaireViewModel(Colis.InventaireJson);
			
			var inventaireWindow = new Views.Inventaire.InventaireView(inventaireViewModel)
			{
				Owner = System.Windows.Application.Current.MainWindow
			};

			if (inventaireWindow.ShowDialog() == true)
			{
				// CORRECTION : Utiliser GetJson() qui force le format camelCase
				Colis.InventaireJson = inventaireViewModel.GetJson();
				
				Colis.NombrePieces = inventaireViewModel.TotalQuantite;
				Colis.ValeurDeclaree = inventaireViewModel.TotalValeur;
				
				// Cette méthode utilise aussi la sérialisation camelCase dans le DTO si vous l'avez configuré
				// Mais ici on a modifié directement l'objet Colis, ce qui est correct.
				
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
			if (Colis == null) return;
			
			var currentStatus = Colis.Statut; // Sauvegarder le statut actuel

			AvailableStatuses.Clear();

			var statuses = new HashSet<StatutColis>
			{
				// Toujours inclure le statut actuel pour éviter l'erreur de binding
				currentStatus, 
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

			// On trie et on ajoute
			foreach (var s in statuses.OrderBy(s => s.ToString()))
			{
				AvailableStatuses.Add(s);
			}
			
			// IMPORTANT : On force la notification du changement de statut pour que la ComboBox se rafraîchisse
			OnPropertyChanged(nameof(Colis)); 
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