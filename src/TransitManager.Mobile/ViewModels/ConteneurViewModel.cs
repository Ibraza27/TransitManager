using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    public partial class ConteneurViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;
        private List<Conteneur> _allConteneurs = new();

        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _searchText = string.Empty;
        
        public ObservableCollection<Conteneur> ConteneursList { get; } = new();
        public ObservableCollection<string> StatutsList { get; } = new();

        [ObservableProperty] private string _selectedStatut = "Tous";

        public ConteneurViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
            StatutsList.Add("Tous");
            foreach (var s in Enum.GetNames(typeof(StatutConteneur)))
            {
                StatutsList.Add(s);
            }
        }


        [RelayCommand]
        private async Task LoadConteneursAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try // <-- AJOUTER
            {
                var conteneurs = await _transitApi.GetConteneursAsync();
                _allConteneurs = conteneurs.ToList();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les conteneurs : {ex.Message}", "OK");
            }
            finally // <-- AJOUTER
            {
                IsBusy = false; // <-- S'ASSURER QUE C'EST TOUJOURS EXÉCUTÉ
            }
        }

        partial void OnSearchTextChanged(string value) => ApplyFilters();
        partial void OnSelectedStatutChanged(string value) => ApplyFilters();

        private void ApplyFilters()
        {
            IEnumerable<Conteneur> filtered = _allConteneurs;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(c => 
                    c.NumeroDossier.ToLower().Contains(search) ||
                    (c.NumeroPlomb?.ToLower().Contains(search) ?? false) ||
                    (c.NomCompagnie?.ToLower().Contains(search) ?? false) ||
                    c.Destination.ToLower().Contains(search) ||
                    (c.NomTransitaire?.ToLower().Contains(search) ?? false)
                );
            }

            if (SelectedStatut != "Tous" && Enum.TryParse<StatutConteneur>(SelectedStatut, out var statut))
            {
                filtered = filtered.Where(c => c.Statut == statut);
            }

            ConteneursList.Clear();
            foreach(var c in filtered.OrderByDescending(c => c.DateCreation))
            {
                ConteneursList.Add(c);
            }
        }

        [RelayCommand]
        private async Task GoToDetailsAsync(Conteneur conteneur)
        {
            if (conteneur == null) return;
            await Shell.Current.GoToAsync($"ConteneurDetailPage?conteneurId={conteneur.Id}");
        }

        [RelayCommand]
        private async Task AddConteneurAsync()
        {
            await Shell.Current.GoToAsync("AddEditConteneurPage?conteneurId=");
        }
    }
}