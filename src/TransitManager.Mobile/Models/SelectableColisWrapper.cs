using CommunityToolkit.Mvvm.ComponentModel;
using TransitManager.Core.Entities;

namespace TransitManager.Mobile.Models
{
    public partial class SelectableColisWrapper : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public Colis Item { get; }

        public SelectableColisWrapper(Colis item)
        {
            Item = item;
        }
    }
}