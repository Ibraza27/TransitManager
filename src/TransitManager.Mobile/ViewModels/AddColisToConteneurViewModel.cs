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
    public partial class AddColisToConteneurViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;
        private List<Colis> _allAvailableColis = new();
        
        [ObservableProperty] private string _conteneurId = string.Empty;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<SelectableColisWrapper> ColisList { get; } = new();

        public AddColisToConteneurViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        async partial void OnConteneurIdChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                await LoadColisAsync();
            }
        }

        private async Task LoadColisAsync()
        {
            IsBusy = true;
            try
            {
                var allColisDtos = await _transitApi.GetColisAsync();
                _allAvailableColis.Clear();

                foreach(var dto in allColisDtos)
                {
                    if(dto.Statut == StatutColis.EnAttente)
                    {
                        var fullColis = await _transitApi.GetColisByIdAsync(dto.Id);
                        _allAvailableColis.Add(fullColis);
                    }
                }
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
            var filtered = _allAvailableColis.AsEnumerable();
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
        private async Task AddSelectedAsync()
        {
            var selectedColisIds = ColisList.Where(w => w.IsSelected).Select(w => w.Item.Id).ToList();
            if (!selectedColisIds.Any())
            {
                await Shell.Current.DisplayAlert("Aucune sélection", "Veuillez cocher les colis à ajouter.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                foreach(var colisId in selectedColisIds)
                {
                    var colis = await _transitApi.GetColisByIdAsync(colisId);
                    colis.ConteneurId = Guid.Parse(ConteneurId);
                    colis.Statut = StatutColis.Affecte;
                    
                    var dto = new UpdateColisDto
                    {
                        Id = colis.Id,
                        ClientId = colis.ClientId,
                        ConteneurId = colis.ConteneurId,
                        Statut = colis.Statut,
                        Designation = colis.Designation,
                        DestinationFinale = colis.DestinationFinale,
                        Barcodes = colis.Barcodes.Select(b => b.Value).ToList(),
                        NombrePieces = colis.NombrePieces,
                        Volume = colis.Volume,
                        ValeurDeclaree = colis.ValeurDeclaree,
                        PrixTotal = colis.PrixTotal,
                        SommePayee = colis.SommePayee,
                        Destinataire = colis.Destinataire,
                        TelephoneDestinataire = colis.TelephoneDestinataire,
                        LivraisonADomicile = colis.LivraisonADomicile,
                        AdresseLivraison = colis.AdresseLivraison,
                        EstFragile = colis.EstFragile,
                        ManipulationSpeciale = colis.ManipulationSpeciale,
                        InstructionsSpeciales = colis.InstructionsSpeciales,
                        Type = colis.Type,
                        TypeEnvoi = colis.TypeEnvoi,
                        InventaireJson = colis.InventaireJson
                    };
                    await _transitApi.UpdateColisAsync(colis.Id, dto);
                }
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible d'ajouter les colis : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // --- DÉBUT DE LA CORRECTION ---
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
        // --- FIN DE LA CORRECTION ---
    }
}