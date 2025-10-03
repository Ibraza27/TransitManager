using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;
using CommunityToolkit.Mvvm.Messaging; // <-- AJOUTER
using TransitManager.Core.Messages; // <-- AJOUTE

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(VehiculeIdStr), "vehiculeId")]
	public partial class VehiculeDetailViewModel : ObservableObject, IRecipient<EntityTotalPaidUpdatedMessage>
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private Vehicule? _vehicule;

        [ObservableProperty]
        private string _vehiculeIdStr = string.Empty;
		
		private bool _isNavigatedBack = false;

        public VehiculeDetailViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
			WeakReferenceMessenger.Default.Register<EntityTotalPaidUpdatedMessage>(this);
        }
		
		public void Receive(EntityTotalPaidUpdatedMessage message)
		{
			// Si le message concerne le véhicule actuel, on met à jour son total payé
			if (Vehicule != null && Vehicule.Id == message.EntityId)
			{
				Vehicule.SommePayee = message.NewTotalPaid;
			}
		}

        async partial void OnVehiculeIdStrChanged(string value)
        {
            if (Guid.TryParse(value, out Guid vehiculeId))
            {
                // Ne charger que si ce n'est pas un retour de navigation
                if (!_isNavigatedBack)
                {
                    await LoadVehiculeDetailsAsync(vehiculeId);
                }
                _isNavigatedBack = false; // Réinitialiser le drapeau
            }
        }

        // --- AJOUTER CETTE COMMANDE ---
        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (!string.IsNullOrEmpty(VehiculeIdStr) && Guid.TryParse(VehiculeIdStr, out Guid vehiculeId))
            {
                await LoadVehiculeDetailsAsync(vehiculeId);
            }
        }

        private async Task LoadVehiculeDetailsAsync(Guid vehiculeId)
        {
            try
            {
                Vehicule = await _transitApi.GetVehiculeByIdAsync(vehiculeId);
            }
            catch (System.Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les détails : {ex.Message}", "OK");
            }
        }
        
        [RelayCommand]
        async Task EditAsync()
        {
            if (Vehicule == null) return;
            _isNavigatedBack = true; // Indiquer que la prochaine apparition est un retour
            await Shell.Current.GoToAsync($"AddEditVehiculePage?vehiculeId={Vehicule.Id}");
        }
        [RelayCommand]
        async Task ViewEtatDesLieuxAsync()
        {
            if (Vehicule == null) return;
			_isNavigatedBack = true;

            var serializerOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };
            
            var vehiculeJson = JsonSerializer.Serialize(Vehicule, serializerOptions);
            await Shell.Current.GoToAsync($"EtatDesLieuxPage?vehiculeJson={Uri.EscapeDataString(vehiculeJson)}");
        }
		
		[RelayCommand]
		async Task EditEtatDesLieuxAsync()
		{
			if (Vehicule == null) return;
			_isNavigatedBack = true;
			var vehiculeJson = JsonSerializer.Serialize(Vehicule, new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve });
			await Shell.Current.GoToAsync($"EditEtatDesLieuxPage?vehiculeJson={Uri.EscapeDataString(vehiculeJson)}");
		}
		
		[RelayCommand]
		async Task GoToPaiementsAsync()
		{
			if (Vehicule == null) return;
			await Shell.Current.GoToAsync($"PaiementVehiculePage?vehiculeId={Vehicule.Id}&prixTotal={Vehicule.PrixTotal}");
		}
    }
}