using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    public partial class ColisViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<ColisListItemDto> ColisList { get; } = new();

        public ColisViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }
		
		public bool IsDataLoaded { get; private set; }

        [RelayCommand]
        private async Task LoadColisAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                ColisList.Clear();
                var colis = await _transitApi.GetColisAsync();
                foreach (var c in colis)
                {
                    ColisList.Add(c);
                }
				IsDataLoaded = true;
            }
            catch (System.Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les colis : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
		
		[RelayCommand]
		private async Task GoToDetailsAsync(ColisListItemDto colis)
		{
			if (colis == null) return;
			await Shell.Current.GoToAsync($"ColisDetailPage?colisId={colis.Id}");
		}
		
		[RelayCommand]
		private async Task AddColisAsync()
		{
			await Shell.Current.GoToAsync("AddEditColisPage");
		}
    }
}