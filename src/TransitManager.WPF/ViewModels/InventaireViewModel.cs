using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using TransitManager.Core.Entities;
using System.Collections.Specialized;
using System.Collections.Generic;

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
            Items.CollectionChanged += OnItemsCollectionChanged;

            LoadItems(inventaireJson);
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

        private void LoadItems(string? json)
        {
            // On s'assure de se désabonner des anciens items avant de vider la liste
            foreach(var item in Items) item.PropertyChanged -= OnItemPropertyChanged;
            Items.Clear();

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var items = System.Text.Json.JsonSerializer.Deserialize<List<InventaireItem>>(json);
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            Items.Add(item); // L'événement CollectionChanged s'occupera de s'abonner
                        }
                    }
                }
                catch { /* Ignorer les erreurs de JSON invalide */ }
            }
        }

        private bool CanAddItem()
        {
            return !string.IsNullOrWhiteSpace(NewItem.Designation) && NewItem.Quantite > 0;
        }

		private void AddItem()
		{
			Items.Add(NewItem);
			NewItem = new InventaireItem(); // Cette ligne crée un nouvel item avec la date du jour
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