using CommunityToolkit.Mvvm.ComponentModel;
using TransitManager.Core.Entities;

namespace TransitManager.WPF.Models
{
    public class SelectableColisWrapper : ObservableObject
    {
        public Colis Colis { get; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public SelectableColisWrapper(Colis colis)
        {
            Colis = colis;
        }
    }
}