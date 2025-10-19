using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Mobile.Services;
using System.Text.Json;

namespace TransitManager.Mobile.ViewModels
{

	[QueryProperty(nameof(VehiculeId), "vehiculeId")]
	[QueryProperty(nameof(PrixTotalVehicule), "prixTotal")]
	[QueryProperty(nameof(UpdatedPaiement), "UpdatedPaiement")]
	public partial class PaiementVehiculeViewModel : ObservableObject
	{
		private readonly ITransitApi _transitApi;
		
		[ObservableProperty] private string _vehiculeId = string.Empty;
		[ObservableProperty] private string _prixTotalVehicule = string.Empty;
		
        [ObservableProperty]
        private string _selectedNewPaymentType = "Especes"; // Valeur par défaut
		
        public List<string> PaymentTypes { get; } = Enum.GetNames(typeof(TypePaiement)).ToList();
		
		[ObservableProperty] private Paiement _newPaiement = new();
		public ObservableCollection<Paiement> Paiements { get; } = new();
		
		[ObservableProperty]
		private Paiement? _updatedPaiement;
		
		// Propriétés pour les statistiques
		[ObservableProperty] private decimal _totalPaye;
		[ObservableProperty] private decimal _restantAPayer;
		
		
		public IAsyncRelayCommand<Paiement> UpdatePaiementCommand { get; }

		public PaiementVehiculeViewModel(ITransitApi transitApi)
		{
			_transitApi = transitApi;
			NewPaiement.DatePaiement = DateTime.Now;
			UpdatePaiementCommand = new AsyncRelayCommand<Paiement>(UpdatePaiementAsync);
		}
		
		async partial void OnUpdatedPaiementChanged(Paiement? value)
		{
			if (value == null) return;
			try
			{
				await _transitApi.UpdatePaiementAsync(value.Id, value);
				// Recharger la liste pour refléter le changement
				await LoadPaiementsAsync(value.VehiculeId.GetValueOrDefault());
			}
			catch (Exception ex)
			{
				await Shell.Current.DisplayAlert("Erreur", $"La mise à jour a échoué: {ex.Message}", "OK");
			}
		}

		// --- NOUVELLE COMMANDE ---
		[RelayCommand]
		private async Task GoToEditPaiementAsync(Paiement paiement)
		{
			var navigationParameter = new Dictionary<string, object>
			{
				{ "paiementJson", JsonSerializer.Serialize(paiement) }
			};
			await Shell.Current.GoToAsync("AddEditPaiementPage", navigationParameter);
		}

		async partial void OnVehiculeIdChanged(string value)
		{
			if (Guid.TryParse(value, out Guid id))
			{
				await LoadPaiementsAsync(id);
			}
		}

		private async Task LoadPaiementsAsync(Guid id)
		{
			Paiements.Clear();
			var paiements = await _transitApi.GetPaiementsForVehiculeAsync(id);
			foreach(var p in paiements)
			{
				Paiements.Add(p);
			}
			CalculateTotals();
		}
		
		private void CalculateTotals()
		{
			TotalPaye = Paiements.Sum(p => p.Montant);
			if (decimal.TryParse(PrixTotalVehicule, out decimal total))
			{
				RestantAPayer = total - TotalPaye;
			}
		}

		[RelayCommand]
        private async Task AddPaiementAsync()
        {
            if (NewPaiement.Montant <= 0 || !Guid.TryParse(VehiculeId, out Guid vehiculeId)) return;
            
            var vehicule = await _transitApi.GetVehiculeByIdAsync(vehiculeId);
            if (vehicule?.Client == null) return;
            
            NewPaiement.VehiculeId = vehiculeId;
            NewPaiement.ClientId = vehicule.ClientId;

            // --- CORRECTION APPLIQUÉE ICI ---
            // On convertit la chaîne sélectionnée en Enum avant d'envoyer
            if (Enum.TryParse<TypePaiement>(SelectedNewPaymentType, out var modePaiement))
            {
                NewPaiement.ModePaiement = modePaiement;
            }

            var createdPaiement = await _transitApi.CreatePaiementAsync(NewPaiement);
            Paiements.Add(createdPaiement);
            
            // Réinitialisation
            NewPaiement = new Paiement { DatePaiement = DateTime.Now };
            SelectedNewPaymentType = "Especes";
            CalculateTotals();
        }

		[RelayCommand]
		private async Task DeletePaiementAsync(Paiement paiement)
		{
			if (paiement == null) return;
			await _transitApi.DeletePaiementAsync(paiement.Id);
			Paiements.Remove(paiement);
			CalculateTotals();
		}
		
        private async Task UpdatePaiementAsync(Paiement? paiement)
        {
            if (paiement == null) return;
            try
            {
                await _transitApi.UpdatePaiementAsync(paiement.Id, paiement);
                CalculateTotals(); // Recalculer les totaux après la mise à jour
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"La mise à jour a échoué: {ex.Message}", "OK");
            }
        }
	}
}