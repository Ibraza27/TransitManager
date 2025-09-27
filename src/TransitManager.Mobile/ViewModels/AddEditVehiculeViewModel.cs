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
    public partial class AddEditVehiculeViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private Vehicule? _vehicule;

        [ObservableProperty]
        private string? _vehiculeId;

        [ObservableProperty]
        private string _pageTitle = string.Empty;

        [ObservableProperty]
        private ClientListItemDto? _selectedClient;
        
        [ObservableProperty]
        private bool _isBusy;

        // --- CORRECTION 1 : RÉINTRODUIRE LA PROPRIÉTÉ ---
        [ObservableProperty]
        private bool _destinataireIdentiqueAuClient;

        public ObservableCollection<ClientListItemDto> Clients { get; } = new();

        private bool _isInitialized = false;
		
        // --- AJOUTER CETTE SECTION ---
        public List<string> VehiculeTypes { get; } = 
            Enum.GetNames(typeof(TypeVehicule)).ToList();

        [ObservableProperty]
        private string _selectedVehiculeType;
        // --- FIN DE L'AJOUT ---

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
                await LoadClientsAsync();

                if (string.IsNullOrEmpty(VehiculeId))
                {
                    PageTitle = "Nouveau Véhicule";
                    Vehicule = new Vehicule();
                    DestinataireIdentiqueAuClient = true; // Par défaut, on coche la case pour un nouveau véhicule
                }
                else
                {
                    PageTitle = "Modifier le Véhicule";
                    var id = Guid.Parse(VehiculeId);
                    Vehicule = await _transitApi.GetVehiculeByIdAsync(id);
                    if (Vehicule != null)
                    {
                        SelectedClient = Clients.FirstOrDefault(c => c.Id == Vehicule.ClientId);
						SelectedVehiculeType = Vehicule.Type.ToString();
                        // Logique pour pré-cocher la case si les infos correspondent
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

        private async Task LoadClientsAsync()
        {
            Clients.Clear();
            var clients = await _transitApi.GetClientsAsync();
            foreach (var client in clients)
            {
                Clients.Add(client);
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
        
        partial void OnSelectedClientChanged(ClientListItemDto? value)
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
                // Le DTO n'a que le téléphone principal, on l'utilise
                Vehicule.TelephoneDestinataire = SelectedClient.TelephonePrincipal;
            }
        }
    }
}