using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    public partial class VehiculesViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<VehiculeListItemDto> VehiculesList { get; } = new();

        public VehiculesViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }
		
		public bool IsDataLoaded { get; private set; }

        [RelayCommand]
        private async Task LoadVehiculesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                VehiculesList.Clear();
                var vehicules = await _transitApi.GetVehiculesAsync();
                foreach (var v in vehicules)
                {
                    VehiculesList.Add(v);
                }
				IsDataLoaded = true;
            }
            catch (System.Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les véhicules : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
		
		[RelayCommand]
		private async Task GoToDetailsAsync(VehiculeListItemDto vehicule)
		{
			if (vehicule == null) return;
			await Shell.Current.GoToAsync($"VehiculeDetailPage?vehiculeId={vehicule.Id}");
		}
		
		[RelayCommand]
		private async Task AddVehiculeAsync()
		{
			await Shell.Current.GoToAsync("AddEditVehiculePage");
		}
    }
}