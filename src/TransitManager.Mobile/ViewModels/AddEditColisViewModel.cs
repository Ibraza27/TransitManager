using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ColisId), "colisId")]
    public partial class AddEditColisViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty] private Colis? _colis;
        [ObservableProperty] private string? _colisId;
        [ObservableProperty] private string _pageTitle = string.Empty;
        [ObservableProperty] private ClientListItemDto? _selectedClient;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _destinataireIdentiqueAuClient;
		
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PrixEstime))] // Notifie que PrixEstime doit être recalculé
		private decimal _prixMetreCube;

		public decimal PrixEstime => (Colis?.Volume ?? 0) * PrixMetreCube;

        public ObservableCollection<ClientListItemDto> Clients { get; } = new();
        public ObservableCollection<Barcode> Barcodes { get; } = new();
        
        [ObservableProperty] private string _newBarcodeText = string.Empty;

        private bool _isInitialized = false;

        public AddEditColisViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            IsBusy = true;
            try
            {
                await LoadClientsAsync();

                if (string.IsNullOrEmpty(ColisId))
                {
                    PageTitle = "Nouveau Colis";
                    Colis = new Colis();
                    DestinataireIdentiqueAuClient = true;
                }
                else
                {
                    PageTitle = "Modifier le Colis";
                    var id = Guid.Parse(ColisId);
                    Colis = await _transitApi.GetColisByIdAsync(id);
                    if (Colis != null)
                    {
                        SelectedClient = Clients.FirstOrDefault(c => c.Id == Colis.ClientId);
                        Barcodes.Clear();
                        foreach(var b in Colis.Barcodes) Barcodes.Add(b);

                        DestinataireIdentiqueAuClient = SelectedClient != null &&
                                                        Colis.Destinataire == SelectedClient.NomComplet &&
                                                        Colis.TelephoneDestinataire == SelectedClient.TelephonePrincipal;
                    }
                }
                _isInitialized = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadClientsAsync()
        {
            Clients.Clear();
            var clients = await _transitApi.GetClientsAsync();
            foreach (var client in clients) Clients.Add(client);
        }
        
        partial void OnSelectedClientChanged(ClientListItemDto? value)
        {
            if (DestinataireIdentiqueAuClient) UpdateDestinataire();
        }

        partial void OnDestinataireIdentiqueAuClientChanged(bool value)
        {
            UpdateDestinataire();
        }

        private void UpdateDestinataire()
        {
            if (Colis == null) return;
            if (DestinataireIdentiqueAuClient && SelectedClient != null)
            {
                Colis.Destinataire = SelectedClient.NomComplet;
                Colis.TelephoneDestinataire = SelectedClient.TelephonePrincipal;
            }
        }
        
        [RelayCommand(CanExecute = nameof(CanAddBarcode))]
        private void AddBarcode()
        {
            Barcodes.Add(new Barcode { Value = NewBarcodeText });
            NewBarcodeText = string.Empty;
        }
        private bool CanAddBarcode() => !string.IsNullOrWhiteSpace(NewBarcodeText);
        
        [RelayCommand]
        private void RemoveBarcode(Barcode barcode)
        {
            if (barcode != null) Barcodes.Remove(barcode);
        }
		
		[RelayCommand]
		private async Task GenerateBarcodeAsync()
		{
			try
			{
				var newBarcodeValue = await _transitApi.GenerateBarcodeAsync();
				Barcodes.Add(new Barcode { Value = newBarcodeValue });
			}
			catch (Exception ex)
			{
				await Shell.Current.DisplayAlert("Erreur", $"Impossible de générer un code-barres : {ex.Message}", "OK");
			}
		}

		[RelayCommand]
		async Task SaveAsync()
		{
			if (Colis == null || SelectedClient == null)
			{
				await Shell.Current.DisplayAlert("Erreur", "Veuillez sélectionner un client.", "OK");
				return;
			}
            if (string.IsNullOrWhiteSpace(Colis.DestinationFinale))
            {
                await Shell.Current.DisplayAlert("Erreur", "Le champ 'Destination Finale' est obligatoire.", "OK");
                return;
            }

			Colis.ClientId = SelectedClient.Id;

            // On s'assure que chaque code-barres a la référence à l'objet Colis parent.
            foreach (var barcode in Barcodes)
            {
                barcode.ColisId = Colis.Id;
                barcode.Colis = Colis;
            }
            Colis.Barcodes = Barcodes.ToList();

			try
			{
				if (string.IsNullOrEmpty(ColisId))
				{
					await _transitApi.CreateColisAsync(Colis);
				}
				else
				{
					await _transitApi.UpdateColisAsync(Colis.Id, Colis);
				}
				await Shell.Current.GoToAsync("..");
			}
			catch (Exception ex)
			{
				if (ex is Refit.ApiException apiEx)
				{
                    string errorDetails;
                    try
                    {
                        var errorContent = await apiEx.GetContentAsAsync<object>();
                        errorDetails = errorContent?.ToString() ?? "Aucun détail.";
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // CORRECTION APPLIQUÉE ICI
                        errorDetails = apiEx.Content ?? "Impossible de lire le contenu de l'erreur.";
                    }
					await Shell.Current.DisplayAlert("Erreur API", $"Sauvegarde échouée : {apiEx.StatusCode}\n\n{errorDetails}", "OK");
				}
				else
				{
					await Shell.Current.DisplayAlert("Erreur", $"Sauvegarde échouée : {ex.Message}", "OK");
				}
			}
		}
		
		// Se déclenche quand la propriété Colis.Volume change
		partial void OnColisChanged(Colis? value)
		{
			if (value != null)
			{
				value.PropertyChanged += (s, e) => {
					if (e.PropertyName == nameof(Colis.Volume))
					{
						OnPropertyChanged(nameof(PrixEstime));
					}
				};
			}
		}		
		
    }
}