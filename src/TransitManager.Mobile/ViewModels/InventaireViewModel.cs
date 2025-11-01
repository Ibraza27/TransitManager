using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ColisId), "colisId")]
    public partial class InventaireViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;
        private Colis? _colis;

        [ObservableProperty]
        private string _colisId = string.Empty;

        public ObservableCollection<InventaireItem> Items { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddItemCommand))]
        private InventaireItem _newItem = new();

        [ObservableProperty]
        private int _totalQuantite;

        [ObservableProperty]
        private decimal _totalValeur;

        public InventaireViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
            Items.CollectionChanged += (s, e) => CalculateTotals();
        }

        async partial void OnColisIdChanged(string value)
        {
            if (Guid.TryParse(value, out var id))
            {
                await LoadItemsFromApiAsync(id);
            }
        }

        private async Task LoadItemsFromApiAsync(Guid id)
        {
            try
            {
                _colis = await _transitApi.GetColisByIdAsync(id);
                if (_colis != null)
                {
                    await LoadItemsFromJsonAsync(_colis.InventaireJson);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les données du colis : {ex.Message}", "OK");
            }
        }

        // --- DÉBUT DE LA CORRECTION ---
        private async Task LoadItemsFromJsonAsync(string? json)
        {
            try
            {
                Items.Clear();
                if (!string.IsNullOrEmpty(json) && json != "[]")
                {
                    var items = JsonSerializer.Deserialize<List<InventaireItem>>(json);
                    if (items != null)
                    {
                        foreach (var item in items) Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                // Cet appel 'await' est la raison de l'erreur
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger l'inventaire : {ex.Message}", "OK");
            }
            
            CalculateTotals();
            // Pas besoin de retourner Task.CompletedTask, la méthode est déjà async.
        }
        // --- FIN DE LA CORRECTION ---

        private void CalculateTotals()
        {
            TotalQuantite = Items.Sum(i => i.Quantite);
            TotalValeur = Items.Sum(i => i.Valeur);
        }

        [RelayCommand(CanExecute = nameof(CanAddItem))]
        private void AddItem()
        {
            Items.Add(NewItem);
            NewItem = new InventaireItem();
        }

        private bool CanAddItem() => !string.IsNullOrWhiteSpace(NewItem.Designation) && NewItem.Quantite > 0;

        [RelayCommand]
        private async Task EditItemAsync(InventaireItem itemToEdit)
        {
            try
            {
                var newDesignation = await Shell.Current.DisplayPromptAsync("Modifier", "Nouvelle désignation :", "OK", "Annuler", initialValue: itemToEdit.Designation);
                if (newDesignation == null) return;

                var newQuantiteStr = await Shell.Current.DisplayPromptAsync("Modifier", "Nouvelle quantité :", "OK", "Annuler", initialValue: itemToEdit.Quantite.ToString(), keyboard: Keyboard.Numeric);
                if (newQuantiteStr == null || !int.TryParse(newQuantiteStr, out var newQuantite)) return;
                
                var newValeurStr = await Shell.Current.DisplayPromptAsync("Modifier", "Nouvelle valeur :", "OK", "Annuler", initialValue: itemToEdit.Valeur.ToString(), keyboard: Keyboard.Numeric);
                if (newValeurStr == null || !decimal.TryParse(newValeurStr, out var newValeur)) return;

                itemToEdit.Designation = newDesignation;
                itemToEdit.Quantite = newQuantite;
                itemToEdit.Valeur = newValeur;
                
                var index = Items.IndexOf(itemToEdit);
                if (index != -1) Items[index] = itemToEdit;
                
                CalculateTotals();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"La modification a échoué : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private void DeleteItem(InventaireItem itemToDelete)
        {
            if (itemToDelete != null) Items.Remove(itemToDelete);
        }

        [RelayCommand]
        private async Task SaveAndCloseAsync()
        {
            if (_colis == null)
            {
                await Shell.Current.DisplayAlert("Erreur", "Les données du colis original sont introuvables.", "OK");
                return;
            }

            try
            {
                foreach (var item in Items)
                {
                    if (item.Date.Kind != DateTimeKind.Utc)
                    {
                        item.Date = item.Date.ToUniversalTime();
                    }
                }

                var updatedJson = JsonSerializer.Serialize(Items);
                
                var inventaireDto = new UpdateInventaireDto
                {
                    ColisId = _colis.Id,
                    InventaireJson = updatedJson,
                    TotalPieces = TotalQuantite,
                    TotalValeurDeclaree = TotalValeur
                };

                await _transitApi.UpdateInventaireAsync(inventaireDto);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur de Sauvegarde", $"L'enregistrement a échoué : {ex.Message}", "OK");
            }
        }
    }
}