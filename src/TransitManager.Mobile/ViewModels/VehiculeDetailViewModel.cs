using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(VehiculeIdStr), "vehiculeId")]
    public partial class VehiculeDetailViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private Vehicule? _vehicule;

        [ObservableProperty]
        private string _vehiculeIdStr = string.Empty;

        public VehiculeDetailViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        async partial void OnVehiculeIdStrChanged(string value)
        {
            if (Guid.TryParse(value, out Guid vehiculeId))
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
            await Shell.Current.GoToAsync($"AddEditVehiculePage?vehiculeId={Vehicule.Id}");
        }

        [RelayCommand]
        async Task ViewEtatDesLieuxAsync()
        {
            if (Vehicule == null) return;

            var serializerOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };
            
            var vehiculeJson = JsonSerializer.Serialize(Vehicule, serializerOptions);
            await Shell.Current.GoToAsync($"EtatDesLieuxPage?vehiculeJson={Uri.EscapeDataString(vehiculeJson)}");
        }
    }
}