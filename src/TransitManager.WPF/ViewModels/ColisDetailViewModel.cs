using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
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

        private Colis? _colis;
        public Colis? Colis { get => _colis; set => SetProperty(ref _colis, value); }

        private ObservableCollection<Client> _clients = new();
        public ObservableCollection<Client> Clients { get => _clients; set => SetProperty(ref _clients, value); }

        private string _newBarcode = string.Empty;
        public string NewBarcode { get => _newBarcode; set => SetProperty(ref _newBarcode, value); }

        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public IRelayCommand AddBarcodeCommand { get; }
        public IRelayCommand<Barcode> RemoveBarcodeCommand { get; }
        public IRelayCommand GenerateBarcodeCommand { get; }

        public ColisDetailViewModel(IColisService colisService, IClientService clientService, IBarcodeService barcodeService, INavigationService navigationService, IDialogService dialogService)
        {
            _colisService = colisService;
            _clientService = clientService;
            _barcodeService = barcodeService;
            _navigationService = navigationService;
            _dialogService = dialogService;

            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            AddBarcodeCommand = new RelayCommand(AddBarcode, () => !string.IsNullOrWhiteSpace(NewBarcode));
            RemoveBarcodeCommand = new RelayCommand<Barcode>(RemoveBarcode);
            GenerateBarcodeCommand = new RelayCommand(GenerateBarcode);
        }

        private bool CanSave()
        {
            return Colis != null &&
                   Colis.ClientId != Guid.Empty &&
                   !string.IsNullOrWhiteSpace(Colis.Destinataire) &&
                   Colis.NombrePieces > 0 &&
                   !IsBusy;
        }

        private async Task SaveAsync()
        {
            if (!CanSave()) return;
            await ExecuteBusyActionAsync(async () =>
            {
                try
                {
                    bool isNew = Colis!.CreePar == null;
                    if (isNew) await _colisService.CreateAsync(Colis);
                    else await _colisService.UpdateAsync(Colis);
                    
                    await _dialogService.ShowInformationAsync("Succès", "Le colis a été enregistré.");
                    _navigationService.NavigateTo("Colis");
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", $"Erreur d'enregistrement : {ex.Message}");
                }
            });
        }

        private void Cancel() => _navigationService.GoBack();

        private void AddBarcode()
        {
            if (!string.IsNullOrWhiteSpace(NewBarcode) && Colis != null)
            {
                Colis.Barcodes.Add(new Barcode { Value = NewBarcode });
                NewBarcode = string.Empty;
            }
        }

        private void RemoveBarcode(Barcode? barcode)
        {
            if (barcode != null && Colis != null)
            {
                Colis.Barcodes.Remove(barcode);
            }
        }

        private void GenerateBarcode()
        {
            if (Colis != null)
            {
                Colis.Barcodes.Add(new Barcode { Value = _barcodeService.GenerateBarcode() });
            }
        }

        private async Task LoadClientsAsync()
        {
            var clientsList = await _clientService.GetActiveClientsAsync();
            Clients = new ObservableCollection<Client>(clientsList);
        }
        
        private void OnColisPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SaveCommand.NotifyCanExecuteChanged();
            if (e.PropertyName == nameof(Colis.TypeEnvoi) || e.PropertyName == nameof(Colis.LivraisonADomicile))
            {
                CalculatePrice();
            }
        }

        private void CalculatePrice()
        {
            if (Colis == null) return;
            decimal prix = 0;
            // Logique de prix simple (à adapter)
            if (Colis.TypeEnvoi == TypeEnvoi.AvecDedouanement) prix += 50m; else prix += 10m;
            if (Colis.LivraisonADomicile) prix += 15m;
            prix += Colis.Poids * 2.5m; // Exemple : 2.5€ par kg

            Colis.PrixTotal = prix;
        }

        public async Task InitializeAsync(string newMarker)
        {
            if (newMarker == "new")
            {
                Title = "Nouveau Colis";
                Colis = new Colis();
                await LoadClientsAsync();
                Colis.PropertyChanged += OnColisPropertyChanged;
            }
        }

        public async Task InitializeAsync(Guid colisId)
        {
            Title = "Modifier le Colis";
            await ExecuteBusyActionAsync(async () =>
            {
                await LoadClientsAsync();
                Colis = await _colisService.GetByIdAsync(colisId);
                if (Colis != null)
                {
                    Colis.PropertyChanged += OnColisPropertyChanged;
                }
            });
        }
    }
}