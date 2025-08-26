using CommunityToolkit.Mvvm.ComponentModel;
using TransitManager.Core.Entities;

namespace TransitManager.WPF.Models
{
    public class SelectableVehiculeWrapper : ObservableObject
    {
        public Vehicule Vehicule { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public SelectableVehiculeWrapper(Vehicule vehicule)
        {
            Vehicule = vehicule;
        }
    }
}