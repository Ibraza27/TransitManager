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
            set => SetProperty(ref _colis, value);
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

        #region Commandes
        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand AddBarcodeCommand { get; }
        public IRelayCommand<Barcode> RemoveBarcodeCommand { get; }
        public IRelayCommand GenerateBarcodeCommand { get; }
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
            GenerateBarcodeCommand = new RelayCommand(GenerateBarcode);
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

			// --- NOUVELLE LOGIQUE DE STATUT ---
			// Si on affecte à un conteneur, le statut devient Affecte.
			// Si on le retire, il redevient EnAttente.
			if (Colis.ConteneurId.HasValue && Colis.Statut == StatutColis.EnAttente)
			{
				Colis.Statut = StatutColis.Affecte;
			}
			else if (!Colis.ConteneurId.HasValue && Colis.Statut == StatutColis.Affecte)
			{
				Colis.Statut = StatutColis.EnAttente;
			}

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
					_navigationService.GoBack(); // <-- On ferme la fenêtre après l'enregistrement
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
            /*
            if (e.PropertyName is nameof(Colis.TypeEnvoi) or nameof(Colis.LivraisonADomicile) or nameof(Colis.Poids))
            {
                CalculatePrice();
            }
            */
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
                    Colis.PropertyChanged += OnColisPropertyChanged;
                    await LoadConteneursDisponiblesAsync();
                    if (Colis.ConteneurId.HasValue)
                    {
                        SelectedConteneur = ConteneursDisponibles.FirstOrDefault(c => c.Id == Colis.ConteneurId.Value);
                    }
                }
            });
        }

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
