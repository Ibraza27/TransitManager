using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ColisId), "colisId")]
    [QueryProperty(nameof(PrixTotalColis), "prixTotal")]
    [QueryProperty(nameof(UpdatedPaiement), "UpdatedPaiement")]
    public partial class PaiementColisViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty] private string _colisId = string.Empty;
        [ObservableProperty] private string _prixTotalColis = string.Empty;
        
        [ObservableProperty] private string _selectedNewPaymentType = "Especes";
        public List<string> PaymentTypes { get; } = Enum.GetNames(typeof(TypePaiement)).ToList();
        
        [ObservableProperty] private Paiement _newPaiement = new();
        public ObservableCollection<Paiement> Paiements { get; } = new();
        
        [ObservableProperty] private Paiement? _updatedPaiement;
        
        [ObservableProperty] private decimal _totalPaye;
        [ObservableProperty] private decimal _restantAPayer;

        public PaiementColisViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
            NewPaiement.DatePaiement = DateTime.Now;
        }

        async partial void OnUpdatedPaiementChanged(Paiement? value)
        {
            if (value == null) return;
            try
            {
                await _transitApi.UpdatePaiementAsync(value.Id, value);
                await LoadPaiementsAsync(value.ColisId.GetValueOrDefault());
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"La mise à jour a échoué: {ex.Message}", "OK");
            }
        }
        
        async partial void OnColisIdChanged(string value)
        {
            if (Guid.TryParse(value, out Guid id))
            {
                await LoadPaiementsAsync(id);
            }
        }

        private async Task LoadPaiementsAsync(Guid id)
        {
            try
            {
                Paiements.Clear();
                var paiements = await _transitApi.GetPaiementsForColisAsync(id);
                foreach(var p in paiements)
                {
                    Paiements.Add(p);
                }
                CalculateTotals();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les paiements : {ex.Message}", "OK");
            }
        }
        
        private void CalculateTotals()
        {
            TotalPaye = Paiements.Sum(p => p.Montant);
            if (decimal.TryParse(PrixTotalColis, out decimal total))
            {
                RestantAPayer = total - TotalPaye;
            }
        }

        [RelayCommand]
        private async Task GoToEditPaiementAsync(Paiement paiement)
        {
            try
            {
                var navigationParameter = new Dictionary<string, object>
                {
                    { "paiementJson", JsonSerializer.Serialize(paiement) }
                };
                await Shell.Current.GoToAsync("AddEditPaiementPage", navigationParameter);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur de Navigation", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task AddPaiementAsync()
        {
            if (NewPaiement.Montant <= 0 || !Guid.TryParse(ColisId, out Guid colisId)) return;
            
            try
            {
                var colis = await _transitApi.GetColisByIdAsync(colisId);
                if (colis?.Client == null) return;

                NewPaiement.ColisId = colisId;
                NewPaiement.ClientId = colis.ClientId;

                if (Enum.TryParse<TypePaiement>(SelectedNewPaymentType, out var modePaiement))
                {
                    NewPaiement.ModePaiement = modePaiement;
                }

                var createdPaiement = await _transitApi.CreatePaiementAsync(NewPaiement);
                Paiements.Add(createdPaiement);
                
                NewPaiement = new Paiement { DatePaiement = DateTime.Now };
                SelectedNewPaymentType = "Especes";
                CalculateTotals();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible d'ajouter le paiement : {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task DeletePaiementAsync(Paiement paiement)
        {
            if (paiement == null) return;
            try
            {
                await _transitApi.DeletePaiementAsync(paiement.Id);
                Paiements.Remove(paiement);
                CalculateTotals();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de supprimer le paiement : {ex.Message}", "OK");
            }
        }
    }
}