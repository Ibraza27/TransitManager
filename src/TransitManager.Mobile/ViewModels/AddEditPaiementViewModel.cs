using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(PaiementJson), "paiementJson")]
    public partial class AddEditPaiementViewModel : ObservableObject
    {
        [ObservableProperty]
        private Paiement? _paiement;
        
        [ObservableProperty]
        private string _paiementJson = string.Empty;

        public List<string> PaymentTypes { get; } = Enum.GetNames(typeof(TypePaiement)).ToList();
        
        async partial void OnPaiementJsonChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                // Désérialiser le paiement passé en paramètre
                Paiement = JsonSerializer.Deserialize<Paiement>(Uri.UnescapeDataString(value));
            }
        }

        [RelayCommand]
        async Task SaveAsync()
        {
            // La sauvegarde se fera via un message de retour
            if (Paiement != null)
            {
                // On encapsule le résultat dans un dictionnaire pour le Shell
                var navigationParameter = new Dictionary<string, object>
                {
                    { "UpdatedPaiement", Paiement }
                };
                await Shell.Current.GoToAsync("..", navigationParameter);
            }
        }
    }
}