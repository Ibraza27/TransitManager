using CommunityToolkit.Mvvm.ComponentModel;
using TransitManager.Core.Entities;

namespace TransitManager.Mobile.Models
{
    public partial class SelectableVehiculeWrapper : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public Vehicule Item { get; }

        public SelectableVehiculeWrapper(Vehicule item)
        {
            Item = item;
        }
    }
}