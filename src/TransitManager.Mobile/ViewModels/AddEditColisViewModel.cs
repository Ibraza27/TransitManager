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
    [QueryProperty(nameof(SelectedClient), "SelectedClient")]
    public partial class AddEditColisViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty] private Colis? _colis;
        [ObservableProperty] private string? _colisId;
        [ObservableProperty] private string _pageTitle = string.Empty;

        [ObservableProperty] private Client? _selectedClient;


        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _destinataireIdentiqueAuClient;
		
		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(PrixEstime))] 
		private decimal _prixMetreCube;

		public decimal PrixEstime => (Colis?.Volume ?? 0) * PrixMetreCube;

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
						SelectedClient = await _transitApi.GetClientByIdAsync(Colis.ClientId);
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

        
        partial void OnSelectedClientChanged(Client? value)
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

			try
			{
				if (string.IsNullOrEmpty(ColisId)) 
				{
					var dto = new CreateColisDto
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
						TypeEnvoi = Colis.TypeEnvoi
					};
					await _transitApi.CreateColisAsync(dto);
				}
				else 
				{
					var dto = new UpdateColisDto
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
                        
                        // --- DÉBUT DE LA CORRECTION ---
                        SommePayee = Colis.SommePayee, // On s'assure de renvoyer la valeur existante
                        // --- FIN DE LA CORRECTION ---

						Destinataire = Colis.Destinataire,
						TelephoneDestinataire = Colis.TelephoneDestinataire,
						LivraisonADomicile = Colis.LivraisonADomicile,
						AdresseLivraison = Colis.AdresseLivraison,
						EstFragile = Colis.EstFragile,
						ManipulationSpeciale = Colis.ManipulationSpeciale,
						InstructionsSpeciales = Colis.InstructionsSpeciales,
						Type = Colis.Type,
						TypeEnvoi = Colis.TypeEnvoi,
                        Statut = Colis.Statut // Il manquait aussi le statut
					};
					await _transitApi.UpdateColisAsync(Colis.Id, dto);
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
		
        [RelayCommand]
        private async Task GoToClientSelectionAsync()
        {
            await Shell.Current.GoToAsync("ClientSelectionPage");
        }
		
    }
}