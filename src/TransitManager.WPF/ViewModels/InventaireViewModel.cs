using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using TransitManager.Core.Entities;
using System.Collections.Specialized;

namespace TransitManager.WPF.ViewModels
{
    public class InventaireViewModel : ObservableObject
    {
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
            
            // On s'abonne à l'événement de changement de la collection
            Items.CollectionChanged += OnItemsCollectionChanged;

            LoadItems(inventaireJson);
        }

        // Nouvelle méthode pour gérer les changements de la collection (ajout/suppression)
        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Se désabonner des anciens éléments
            if (e.OldItems != null)
            {
                foreach (InventaireItem item in e.OldItems)
                {
                    item.PropertyChanged -= OnItemPropertyChanged;
                }
            }
            // S'abonner aux nouveaux éléments
            if (e.NewItems != null)
            {
                foreach (InventaireItem item in e.NewItems)
                {
                    item.PropertyChanged += OnItemPropertyChanged;
                }
            }
            // Mettre à jour les totaux dans tous les cas
            UpdateTotals();
        }

        // Nouvelle méthode pour gérer les changements de propriétés d'un élément (Quantité/Valeur)
        private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InventaireItem.Quantite) || e.PropertyName == nameof(InventaireItem.Valeur))
            {
                UpdateTotals();
            }
        }

        // Méthode centralisée pour mettre à jour les totaux
        private void UpdateTotals()
        {
            OnPropertyChanged(nameof(TotalQuantite));
            OnPropertyChanged(nameof(TotalValeur));
        }

        private void LoadItems(string? json)
        {
            Items.Clear(); // Vider la collection existante
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<InventaireItem>>(json);
                    if (items != null)
                    {
                        // Ajouter les éléments un par un
                        foreach (var item in items)
                        {
                            Items.Add(item);
                        }
                    }
                }
                catch
                {
                    // Ignorer les erreurs de désérialisation, la liste restera vide
                }
            }
        }

        private bool CanAddItem()
        {
            return !string.IsNullOrWhiteSpace(NewItem.Designation) && NewItem.Quantite > 0;
        }

        private void AddItem()
        {
            Items.Add(NewItem);
            NewItem = new InventaireItem();
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