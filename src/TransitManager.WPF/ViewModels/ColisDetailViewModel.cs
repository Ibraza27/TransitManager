// src/TransitManager.WPF/ViewModels/ColisDetailViewModel.cs
using System.Threading.Tasks;

namespace TransitManager.WPF.ViewModels
{
    public class ColisDetailViewModel : BaseViewModel
    {
        public ColisDetailViewModel()
        {
            Title = "Détails du Colis";
        }

        public override Task InitializeAsync()
        {
            // Logique de chargement à venir
            return base.InitializeAsync();
        }
    }
}