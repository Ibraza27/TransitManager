using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Models;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ConteneurId), "conteneurId")]
    public partial class RemoveColisFromConteneurViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;
        private List<Colis> _allAssignedColis = new();

        [ObservableProperty] private string _conteneurId = string.Empty;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<SelectableColisWrapper> ColisList { get; } = new();

        public RemoveColisFromConteneurViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        async partial void OnConteneurIdChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                await LoadAssignedColisAsync();
            }
        }

        private async Task LoadAssignedColisAsync()
        {
            IsBusy = true;
            try
            {
                var conteneur = await _transitApi.GetConteneurByIdAsync(Guid.Parse(ConteneurId));
                _allAssignedColis = conteneur?.Colis.ToList() ?? new List<Colis>();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les colis : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        partial void OnSearchTextChanged(string value) => ApplyFilters();

        private void ApplyFilters()
        {
            var filtered = _allAssignedColis.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(c => 
                    (c.AllBarcodes?.ToLower().Contains(search) ?? false) ||
                    (c.Client?.NomComplet.ToLower().Contains(search) ?? false) ||
                    c.Designation.ToLower().Contains(search) ||
                    c.DestinationFinale.ToLower().Contains(search)
                );
            }

            ColisList.Clear();
            foreach(var colis in filtered)
            {
                ColisList.Add(new SelectableColisWrapper(colis));
            }
        }

        [RelayCommand]
        private async Task RemoveSelectedAsync()
        {
            var selectedColis = ColisList.Where(w => w.IsSelected).Select(w => w.Item).ToList();
            if (!selectedColis.Any())
            {
                await Shell.Current.DisplayAlert("Aucune sélection", "Veuillez cocher les colis à retirer.", "OK");
                return;
            }

            bool confirm = await Shell.Current.DisplayAlert("Confirmation", $"Voulez-vous vraiment retirer {selectedColis.Count} colis de ce conteneur ?", "Oui", "Non");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                foreach(var colis in selectedColis)
                {
                    colis.ConteneurId = null;
                    colis.Statut = Core.Enums.StatutColis.EnAttente;

                    var dto = new UpdateColisDto { /* ... mapper toutes les propriétés ici comme nous l'avons fait précédemment ... */ };
                    // Pour la simplicité, nous allons recréer le DTO complet
                    var fullColis = await _transitApi.GetColisByIdAsync(colis.Id);
                    var updateDto = new UpdateColisDto
                    {
                        Id = fullColis.Id, ClientId = fullColis.ClientId, ConteneurId = null, Statut = Core.Enums.StatutColis.EnAttente,
                        Designation = fullColis.Designation, DestinationFinale = fullColis.DestinationFinale, Barcodes = fullColis.Barcodes.Select(b => b.Value).ToList(),
                        NombrePieces = fullColis.NombrePieces, Volume = fullColis.Volume, ValeurDeclaree = fullColis.ValeurDeclaree,
                        PrixTotal = fullColis.PrixTotal, SommePayee = fullColis.SommePayee, Destinataire = fullColis.Destinataire,
                        TelephoneDestinataire = fullColis.TelephoneDestinataire, LivraisonADomicile = fullColis.LivraisonADomicile,
                        AdresseLivraison = fullColis.AdresseLivraison, EstFragile = fullColis.EstFragile, ManipulationSpeciale = fullColis.ManipulationSpeciale,
                        InstructionsSpeciales = fullColis.InstructionsSpeciales, Type = fullColis.Type, TypeEnvoi = fullColis.TypeEnvoi, InventaireJson = fullColis.InventaireJson
                    };
                    await _transitApi.UpdateColisAsync(colis.Id, updateDto);
                }
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de retirer les colis : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoToColisDetailsAsync(object? obj)
        {
            if (obj is not SelectableColisWrapper wrapper || wrapper.Item == null) return;
            try
            {
                await Shell.Current.GoToAsync($"ColisDetailPage?colisId={wrapper.Item.Id}");
            }
            catch (Exception ex)
            {
                 await Shell.Current.DisplayAlert("Erreur de Navigation", ex.Message, "OK");
            }
        }
    }
}