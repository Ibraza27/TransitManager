using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;
using TransitManager.Core.Enums;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(VehiculeId), "vehiculeId")]
	[QueryProperty(nameof(SelectedClient), "SelectedClient")]
    public partial class AddEditVehiculeViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private Vehicule? _vehicule;

        [ObservableProperty]
        private string? _vehiculeId;

        [ObservableProperty]
        private string _pageTitle = string.Empty;

        // --- DÉBUT DE LA MODIFICATION 1 ---
        [ObservableProperty]
        private Client? _selectedClient;
        
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _destinataireIdentiqueAuClient;


        // --- FIN DE LA MODIFICATION 1 ---

        private bool _isInitialized = false;
		
        public List<string> VehiculeTypes { get; } = 
            Enum.GetNames(typeof(TypeVehicule)).ToList();

        [ObservableProperty]
        private string _selectedVehiculeType = string.Empty; // Initialiser avec une chaîne vide

        public AddEditVehiculeViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;
            IsBusy = true;
            try
            {

                if (string.IsNullOrEmpty(VehiculeId))
                {
                    PageTitle = "Nouveau Véhicule";
                    Vehicule = new Vehicule();
                    DestinataireIdentiqueAuClient = true; 
                }
                else
                {
                    PageTitle = "Modifier le Véhicule";
                    var id = Guid.Parse(VehiculeId);
                    Vehicule = await _transitApi.GetVehiculeByIdAsync(id);
                    if (Vehicule != null)
                    {
                        SelectedClient = await _transitApi.GetClientByIdAsync(Vehicule.ClientId);
						SelectedVehiculeType = Vehicule.Type.ToString();
                        DestinataireIdentiqueAuClient = SelectedClient != null &&
                                                        Vehicule.Destinataire == SelectedClient.NomComplet &&
                                                        Vehicule.TelephoneDestinataire == SelectedClient.TelephonePrincipal;
                    }
                }
                _isInitialized = true;
            }
            finally
            {
                IsBusy = false;
            }
        }
		
		partial void OnSelectedVehiculeTypeChanged(string value)
		{
			if (Vehicule != null && !string.IsNullOrEmpty(value))
			{
				Vehicule.Type = (TypeVehicule)Enum.Parse(typeof(TypeVehicule), value);
			}
		}


        [RelayCommand]
        async Task SaveAsync()
        {
            if (Vehicule == null || SelectedClient == null)
            {
                await Shell.Current.DisplayAlert("Erreur", "Veuillez sélectionner un client.", "OK");
                return;
            }

            Vehicule.ClientId = SelectedClient.Id;

            try
            {
                if (string.IsNullOrEmpty(VehiculeId))
                {
                    await _transitApi.CreateVehiculeAsync(Vehicule);
                }
                else
                {
                    await _transitApi.UpdateVehiculeAsync(Vehicule.Id, Vehicule);
                }
                await Shell.Current.GoToAsync("..");
            }
            catch (System.Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Sauvegarde échouée : {ex.Message}", "OK");
            }
        }
        
        // --- DÉBUT DE LA MODIFICATION 2 ---
        partial void OnSelectedClientChanged(Client? value)
        // --- FIN DE LA MODIFICATION 2 ---
        {
            if (DestinataireIdentiqueAuClient)
            {
                UpdateDestinataire();
            }
        }

        partial void OnDestinataireIdentiqueAuClientChanged(bool value)
        {
            UpdateDestinataire();
        }

        private void UpdateDestinataire()
        {
            if (Vehicule == null) return;
            
            if (DestinataireIdentiqueAuClient && SelectedClient != null)
            {
                Vehicule.Destinataire = SelectedClient.NomComplet;
                Vehicule.TelephoneDestinataire = SelectedClient.TelephonePrincipal;
            }
        }
		
        [RelayCommand]
        private async Task GoToClientSelectionAsync()
        {
            await Shell.Current.GoToAsync("ClientSelectionPage");
        }
    }
}