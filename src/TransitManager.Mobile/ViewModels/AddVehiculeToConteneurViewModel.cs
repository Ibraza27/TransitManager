using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Mobile.Models;
using TransitManager.Mobile.Services;
using System.Linq;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ConteneurId), "conteneurId")]
    public partial class AddVehiculeToConteneurViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;
        private List<Vehicule> _allAvailableVehicules = new();

        [ObservableProperty] private string _conteneurId = string.Empty;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<SelectableVehiculeWrapper> VehiculesList { get; } = new();

        public AddVehiculeToConteneurViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        async partial void OnConteneurIdChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                await LoadVehiculesAsync();
            }
        }

        private async Task LoadVehiculesAsync()
        {
            IsBusy = true;
            try
            {
                var allVehiculesDtos = await _transitApi.GetVehiculesAsync();
                _allAvailableVehicules.Clear();

                foreach(var dto in allVehiculesDtos)
                {
                    if(dto.Statut == StatutVehicule.EnAttente)
                    {
                        var vehicule = await _transitApi.GetVehiculeByIdAsync(dto.Id);
                        _allAvailableVehicules.Add(vehicule);
                    }
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les véhicules : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSearchTextChanged(string value) => ApplyFilters();

        private void ApplyFilters()
        {
            var filtered = _allAvailableVehicules.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(v =>
                    v.Immatriculation.ToLower().Contains(search) ||
                    (v.Client?.NomComplet.ToLower().Contains(search) ?? false) ||
					(v.Client?.TelephonePrincipal?.Contains(search) ?? false) ||
                    v.Marque.ToLower().Contains(search) ||
                    v.Modele.ToLower().Contains(search)
                );
            }

            VehiculesList.Clear();
            foreach (var v in filtered)
            {
                VehiculesList.Add(new SelectableVehiculeWrapper(v));
            }
        }

        [RelayCommand]
        private async Task AddSelectedAsync()
        {
            var selectedVehicules = VehiculesList.Where(w => w.IsSelected).Select(w => w.Item).ToList();
            if (!selectedVehicules.Any())
            {
                await Shell.Current.DisplayAlert("Aucune sélection", "Veuillez cocher les véhicules à ajouter.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                foreach (var vehicule in selectedVehicules)
                {
                    vehicule.ConteneurId = Guid.Parse(ConteneurId);
                    await _transitApi.UpdateVehiculeAsync(vehicule.Id, vehicule);
                }
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible d'ajouter les véhicules : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // --- DÉBUT DE LA CORRECTION ---
        [RelayCommand]
        private async Task GoToVehiculeDetailsAsync(object? obj)
        {
            if (obj is not SelectableVehiculeWrapper wrapper || wrapper.Item == null) return;
            
            try
            {
                await Shell.Current.GoToAsync($"VehiculeDetailPage?vehiculeId={wrapper.Item.Id}");
            }
            catch (Exception ex)
            {
                 await Shell.Current.DisplayAlert("Erreur de Navigation", ex.Message, "OK");
            }
        }
        // --- FIN DE LA CORRECTION ---
    }
}