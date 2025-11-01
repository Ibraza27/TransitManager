using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Models;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ConteneurId), "conteneurId")]
    public partial class RemoveVehiculeFromConteneurViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;
        private List<Vehicule> _allAssignedVehicules = new();

        [ObservableProperty] private string _conteneurId = string.Empty;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<SelectableVehiculeWrapper> VehiculesList { get; } = new();

        public RemoveVehiculeFromConteneurViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        async partial void OnConteneurIdChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                await LoadAssignedVehiculesAsync();
            }
        }

        private async Task LoadAssignedVehiculesAsync()
        {
            IsBusy = true;
            try
            {
                var conteneur = await _transitApi.GetConteneurByIdAsync(Guid.Parse(ConteneurId));
                _allAssignedVehicules = conteneur?.Vehicules.ToList() ?? new List<Vehicule>();
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
            var filtered = _allAssignedVehicules.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(v =>
                    v.Immatriculation.ToLower().Contains(search) ||
                    (v.Client?.NomComplet.ToLower().Contains(search) ?? false) ||
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
        private async Task RemoveSelectedAsync()
        {
            var selectedVehicules = VehiculesList.Where(w => w.IsSelected).Select(w => w.Item).ToList();
            if (!selectedVehicules.Any())
            {
                await Shell.Current.DisplayAlert("Aucune sélection", "Veuillez cocher les véhicules à retirer.", "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert("Confirmation", $"Voulez-vous vraiment retirer {selectedVehicules.Count} véhicule(s) de ce conteneur ?", "Oui", "Non");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                foreach (var vehicule in selectedVehicules)
                {
                    vehicule.ConteneurId = null;
                    vehicule.Statut = Core.Enums.StatutVehicule.EnAttente;
                    await _transitApi.UpdateVehiculeAsync(vehicule.Id, vehicule);
                }
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de retirer les véhicules : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

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
    }
}