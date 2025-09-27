using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;

namespace TransitManager.Mobile.ViewModels
{
    [QueryProperty(nameof(ColisId), "colisId")]
    public partial class AddEditColisViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;

        [ObservableProperty]
        private Colis? _colis;

        [ObservableProperty]
        private string? _colisId;

        [ObservableProperty]
        private string _pageTitle = string.Empty;

        [ObservableProperty]
        private ClientListItemDto? _selectedClient;
		
        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<ClientListItemDto> Clients { get; } = new();

        private bool _isInitialized = false;

        public AddEditColisViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            IsBusy = true;
            try
            {
                await LoadClientsAsync();

                if (string.IsNullOrEmpty(ColisId))
                {
                    PageTitle = "Nouveau Colis";
                    Colis = new Colis();
                }
                else
                {
                    PageTitle = "Modifier le Colis";
                    var id = Guid.Parse(ColisId);
                    Colis = await _transitApi.GetColisByIdAsync(id);
                    if (Colis != null)
                    {
                        SelectedClient = Clients.FirstOrDefault(c => c.Id == Colis.ClientId);
                    }
                }
                _isInitialized = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadClientsAsync()
        {
            Clients.Clear();
            var clients = await _transitApi.GetClientsAsync();
            foreach (var client in clients)
            {
                Clients.Add(client);
            }
        }

        [RelayCommand]
        async Task SaveAsync()
        {
            if (Colis == null || SelectedClient == null)
            {
                await Shell.Current.DisplayAlert("Erreur", "Veuillez sélectionner un client.", "OK");
                return;
            }

            Colis.ClientId = SelectedClient.Id;

            try
            {
                if (string.IsNullOrEmpty(ColisId))
                {
                    await _transitApi.CreateColisAsync(Colis);
                }
                else
                {
                    await _transitApi.UpdateColisAsync(Colis.Id, Colis);
                }
                await Shell.Current.GoToAsync("..");
            }
            catch (System.Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", $"Sauvegarde échouée : {ex.Message}", "OK");
            }
        }
    }
}