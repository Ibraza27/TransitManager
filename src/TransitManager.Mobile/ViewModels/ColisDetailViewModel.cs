using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;
using CommunityToolkit.Mvvm.Messaging; 
using TransitManager.Core.Messages;   

namespace TransitManager.Mobile.ViewModels
{
	[QueryProperty(nameof(ColisIdStr), "colisId")]
    // --- DÉBUT DE LA MODIFICATION 1 : Supprimer la QueryProperty ---
	public partial class ColisDetailViewModel : ObservableObject, IRecipient<EntityTotalPaidUpdatedMessage>
    // --- FIN DE LA MODIFICATION 1 ---
	{
		private readonly ITransitApi _transitApi;
		
		[ObservableProperty]
		private Colis? _colis;
		
		[ObservableProperty]
		private string _colisIdStr = string.Empty;
		
		private bool _isNavigatedBack = false;

		public ColisDetailViewModel(ITransitApi transitApi)
		{
			_transitApi = transitApi;
            WeakReferenceMessenger.Default.Register<EntityTotalPaidUpdatedMessage>(this);
		}

        public void Receive(EntityTotalPaidUpdatedMessage message)
        {
            if (Colis != null && Colis.Id == message.EntityId)
            {
                Colis.SommePayee = message.NewTotalPaid;
            }
        }

		async partial void OnColisIdStrChanged(string value)
		{
			if (Guid.TryParse(value, out Guid colisId))
			{
				await LoadColisDetailsAsync(colisId);
			}
		}

		private async Task LoadColisDetailsAsync(Guid colisId)
		{
			try
			{
				Colis = await _transitApi.GetColisByIdAsync(colisId);
			}
			catch (System.Exception ex)
			{
				await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les détails : {ex.Message}", "OK");
			}
		}
		
		[RelayCommand]
		async Task EditAsync()
		{
			if (Colis == null) return;
			_isNavigatedBack = true;
			await Shell.Current.GoToAsync($"AddEditColisPage?colisId={Colis.Id}");
		}
		
		[RelayCommand]
		private async Task RefreshAsync()
		{
			if (!string.IsNullOrEmpty(ColisIdStr) && Guid.TryParse(ColisIdStr, out Guid colisId))
			{
				await LoadColisDetailsAsync(colisId);
			}
		}

        [RelayCommand]
        async Task GoToPaiementsAsync()
        {
            if (Colis == null) return;
            try
            {
                await Shell.Current.GoToAsync($"PaiementColisPage?colisId={Colis.Id}&prixTotal={Colis.PrixTotal}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur de Navigation", ex.Message, "OK");
            }
        }
        
        [RelayCommand]
        async Task GoToInventaireAsync()
        {
            if (Colis == null) return;
            try
            {
                // --- DÉBUT DE LA MODIFICATION 2 : Passer l'ID au lieu du JSON ---
                await Shell.Current.GoToAsync($"InventairePage?colisId={Colis.Id}");
                // --- FIN DE LA MODIFICATION 2 ---
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur de Navigation", ex.Message, "OK");
            }
        }

		[RelayCommand]
		async Task DeleteAsync()
		{
			if (Colis == null) return;
			bool confirm = await Shell.Current.DisplayAlert("Supprimer", $"Êtes-vous sûr de vouloir supprimer {Colis.NumeroReference} ?", "Oui", "Non");
			if (confirm)
			{
                try
                {
				    await _transitApi.DeleteColisAsync(Colis.Id);
				    await Shell.Current.GoToAsync("..");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Erreur", $"La suppression a échoué : {ex.Message}", "OK");
                }
			}
		}
	}
}