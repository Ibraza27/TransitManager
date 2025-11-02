using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization; // <-- AJOUTER CETTE LIGNE
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
        private InventaireItem _newItem = new();

        [ObservableProperty]
        private int _totalQuantite;

        [ObservableProperty]
        private decimal _totalValeur;

        // --- DÉBUT DE L'AJOUT 1 : Options de sérialisation globales pour ce ViewModel ---
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        // --- FIN DE L'AJOUT 1 ---

        public InventaireViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
            Items.CollectionChanged += (s, e) => CalculateTotals();

            // --- DÉBUT DE L'AJOUT 2 : S'abonner manuellement pour une meilleure réactivité ---
            NewItem.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
            // --- FIN DE L'AJOUT 2 ---
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

        private async Task LoadItemsFromJsonAsync(string? json)
        {
            try
            {
                Items.Clear();
                if (!string.IsNullOrEmpty(json) && json != "[]" && json != "null")
                {
                    // --- DÉBUT DE LA CORRECTION 3 : Utiliser les options de sérialisation ---
                    var items = JsonSerializer.Deserialize<List<InventaireItem>>(json, _jsonOptions);
                    // --- FIN DE LA CORRECTION 3 ---
                    
                    if (items != null)
                    {
                        foreach (var item in items) Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur de Désérialisation", $"Impossible de lire l'inventaire : {ex.Message}", "OK");
            }
            
            CalculateTotals();
        }

        private void CalculateTotals()
        {
            TotalQuantite = Items.Sum(i => i.Quantite);
            TotalValeur = Items.Sum(i => i.Valeur);
        }

        [RelayCommand(CanExecute = nameof(CanAddItem))]
        private void AddItem()
        {
            Items.Add(NewItem);

            // Important : désabonner l'ancien objet pour éviter les fuites de mémoire
            NewItem.PropertyChanged -= (s, e) => AddItemCommand.NotifyCanExecuteChanged();
            
            NewItem = new InventaireItem();
            
            // --- DÉBUT DE L'AJOUT 4 : Se réabonner au nouvel objet ---
            NewItem.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
            AddItemCommand.NotifyCanExecuteChanged(); // Mettre à jour l'état du bouton immédiatement
            // --- FIN DE L'AJOUT 4 ---
        }

        private bool CanAddItem() => !string.IsNullOrWhiteSpace(NewItem.Designation) && NewItem.Quantite > 0;

        [RelayCommand]
        private async Task EditItemAsync(InventaireItem itemToEdit)
        {
            try
            {
                // --- DÉBUT DE L'AJOUT 5 : Pouvoir modifier la date ---
                var newDateStr = await Shell.Current.DisplayPromptAsync("Modifier", "Nouvelle date (jj/mm/aaaa) :", "OK", "Annuler", initialValue: itemToEdit.Date.ToString("dd/MM/yyyy"));
                if (newDateStr == null || !DateTime.TryParseExact(newDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var newDate)) return;
                // --- FIN DE L'AJOUT 5 ---

                var newDesignation = await Shell.Current.DisplayPromptAsync("Modifier", "Nouvelle désignation :", "OK", "Annuler", initialValue: itemToEdit.Designation);
                if (newDesignation == null) return;

                var newQuantiteStr = await Shell.Current.DisplayPromptAsync("Modifier", "Nouvelle quantité :", "OK", "Annuler", initialValue: itemToEdit.Quantite.ToString(), keyboard: Keyboard.Numeric);
                if (newQuantiteStr == null || !int.TryParse(newQuantiteStr, out var newQuantite)) return;
                
                var newValeurStr = await Shell.Current.DisplayPromptAsync("Modifier", "Nouvelle valeur :", "OK", "Annuler", initialValue: itemToEdit.Valeur.ToString(), keyboard: Keyboard.Numeric);
                if (newValeurStr == null || !decimal.TryParse(newValeurStr, out var newValeur)) return;

                itemToEdit.Date = newDate; // <-- Appliquer la nouvelle date
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