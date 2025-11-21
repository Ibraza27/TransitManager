using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using TransitManager.Core.Entities;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.Json; // Indispensable
using System.Text.Json.Serialization; // Indispensable

namespace TransitManager.WPF.ViewModels
{
    public class InventaireViewModel : ObservableObject
    {
        // --- CONFIGURATION JSON STRICTE (Identique au Web/Mobile) ---
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,        // Pour lire n'importe quel format
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Pour ÉCRIRE en camelCase (standard Web)
            WriteIndented = false
        };

        private InventaireItem _newItem = new();
        public ObservableCollection<InventaireItem> Items { get; } = new();

        public InventaireItem NewItem
        {
            get => _newItem;
            set => SetProperty(ref _newItem, value);
        }

        public int TotalQuantite => Items.Sum(i => i.Quantite);
        public decimal TotalValeur => Items.Sum(i => i.Valeur);

        public IRelayCommand AddItemCommand { get; }
        public IRelayCommand<InventaireItem> RemoveItemCommand { get; }

        public InventaireViewModel(string? inventaireJson)
        {
            AddItemCommand = new RelayCommand(AddItem, CanAddItem);
            RemoveItemCommand = new RelayCommand<InventaireItem>(RemoveItem);

            NewItem.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
            Items.CollectionChanged += OnItemsCollectionChanged;

            LoadItems(inventaireJson);
        }

        // --- MÉTHODE PUBLIQUE POUR RÉCUPÉRER LE JSON FORMATÉ ---
        public string GetJson()
        {
            // On utilise les options camelCase pour générer le string
            return JsonSerializer.Serialize(Items, _jsonOptions);
        }

        private void LoadItems(string? json)
        {
            foreach(var item in Items) item.PropertyChanged -= OnItemPropertyChanged;
            Items.Clear();

            if (!string.IsNullOrEmpty(json) && json != "[]" && json != "null")
            {
                try
                {
                    // On utilise les options pour lire (insensible à la casse)
                    var items = JsonSerializer.Deserialize<List<InventaireItem>>(json, _jsonOptions);
                    
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            Items.Add(item);
                        }
                    }
                }
                catch { /* Ignorer les erreurs de JSON invalide */ }
            }
            
            UpdateTotals();
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (InventaireItem item in e.OldItems)
                    item.PropertyChanged -= OnItemPropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (InventaireItem item in e.NewItems)
                    item.PropertyChanged += OnItemPropertyChanged;
            }
            UpdateTotals();
        }

        private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventaireItem.Quantite) || e.PropertyName == nameof(InventaireItem.Valeur))
            {
                UpdateTotals();
            }
        }

        private void UpdateTotals()
        {
            OnPropertyChanged(nameof(TotalQuantite));
            OnPropertyChanged(nameof(TotalValeur));
        }

        private bool CanAddItem()
        {
            return !string.IsNullOrWhiteSpace(NewItem.Designation) && NewItem.Quantite > 0;
        }

        private void AddItem()
        {
            Items.Add(NewItem);
            // Recréer un objet propre
            NewItem = new InventaireItem { Date = System.DateTime.Now }; 
            NewItem.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
        }

        private void RemoveItem(InventaireItem? item)
        {
            if (item != null)
            {
                Items.Remove(item);
            }
        }
    }
}