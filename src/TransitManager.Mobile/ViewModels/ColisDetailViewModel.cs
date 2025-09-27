using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
	[QueryProperty(nameof(ColisIdStr), "colisId")]
	public partial class ColisDetailViewModel : ObservableObject
	{
		private readonly ITransitApi _transitApi;
		
		[ObservableProperty]
		private Colis? _colis;
		
		[ObservableProperty]
		private string _colisIdStr = string.Empty;

		public ColisDetailViewModel(ITransitApi transitApi)
		{
			_transitApi = transitApi;
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
			await Shell.Current.GoToAsync($"AddEditColisPage?colisId={Colis.Id}");
		}

		[RelayCommand]
		async Task DeleteAsync()
		{
			if (Colis == null) return;
			bool confirm = await Shell.Current.DisplayAlert("Supprimer", $"Êtes-vous sûr de vouloir supprimer {Colis.NumeroReference} ?", "Oui", "Non");
			if (confirm)
			{
				await _transitApi.DeleteColisAsync(Colis.Id);
				await Shell.Current.GoToAsync("..");
			}
		}
	}
}