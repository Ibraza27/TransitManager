using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs; // <-- AJOUTER CE USING
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    // --- DÉBUT DE LA MODIFICATION 1 : Changer la QueryProperty ---
    [QueryProperty(nameof(ColisId), "colisId")]
    public partial class InventaireViewModel : ObservableObject
    // --- FIN DE LA MODIFICATION 1 ---
    {
        // --- DÉBUT DE LA MODIFICATION 2 : Injecter l'API et ajouter des champs ---
        private readonly ITransitApi _transitApi;
        private Colis? _colis; // Pour conserver l'objet colis complet

        [ObservableProperty]
        private string _colisId = string.Empty;
        // --- FIN DE LA MODIFICATION 2 ---

        public ObservableCollection<InventaireItem> Items { get; } = new();

        [ObservableProperty]
        private InventaireItem _newItem = new();

        [ObservableProperty]
        private int _totalQuantite;

        [ObservableProperty]
        private decimal _totalValeur;

        // --- DÉBUT DE LA MODIFICATION 3 : Mettre à jour le constructeur ---
        public InventaireViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
            Items.CollectionChanged += (s, e) => CalculateTotals();
            NewItem.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
        }
        // --- FIN DE LA MODIFICATION 3 ---

        // --- DÉBUT DE LA MODIFICATION 4 : Remplacer l'ancienne méthode de chargement ---
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
        // --- FIN DE LA MODIFICATION 4 ---

        private Task LoadItemsFromJsonAsync(string? json)
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
                Shell.Current.DisplayAlert("Erreur", $"Impossible de charger l'inventaire : {ex.Message}", "OK");
            }
            
            CalculateTotals();
            return Task.CompletedTask;
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
            NewItem = new InventaireItem(); 
            NewItem.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
            AddItemCommand.NotifyCanExecuteChanged();
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

        // --- DÉBUT DE LA MODIFICATION 5 : Remplacer complètement la méthode de sauvegarde ---
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
                // Mettre à jour les dates en UTC
                foreach (var item in Items)
                {
                    if (item.Date.Kind != DateTimeKind.Utc)
                    {
                        item.Date = item.Date.ToUniversalTime();
                    }
                }

                var updatedJson = JsonSerializer.Serialize(Items);
                
                // Créer un DTO (Data Transfer Object) pour la mise à jour
                var updateDto = new UpdateColisDto
                {
                    Id = _colis.Id,
                    ClientId = _colis.ClientId,
                    Designation = _colis.Designation,
                    DestinationFinale = _colis.DestinationFinale,
                    Barcodes = _colis.Barcodes.Select(b => b.Value).ToList(),
                    PrixTotal = _colis.PrixTotal,
                    Destinataire = _colis.Destinataire,
                    TelephoneDestinataire = _colis.TelephoneDestinataire,
                    LivraisonADomicile = _colis.LivraisonADomicile,
                    AdresseLivraison = _colis.AdresseLivraison,
                    EstFragile = _colis.EstFragile,
                    ManipulationSpeciale = _colis.ManipulationSpeciale,
                    InstructionsSpeciales = _colis.InstructionsSpeciales,
                    Type = _colis.Type,
                    TypeEnvoi = _colis.TypeEnvoi,
                    ConteneurId = _colis.ConteneurId,
                    Statut = _colis.Statut,

                    // Appliquer les changements de l'inventaire
                    InventaireJson = updatedJson,
                    NombrePieces = TotalQuantite,
                    ValeurDeclaree = TotalValeur,
                    SommePayee = _colis.SommePayee, // Conserver le montant déjà payé
                    Volume = _colis.Volume
                };

                // Appeler l'API pour sauvegarder
                await _transitApi.UpdateColisAsync(_colis.Id, updateDto);

                // Revenir à la page précédente
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur de Sauvegarde", $"L'enregistrement a échoué : {ex.Message}", "OK");
            }
        }
        // --- FIN DE LA MODIFICATION 5 ---
    }
}