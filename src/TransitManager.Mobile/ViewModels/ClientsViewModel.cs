using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TransitManager.Core.Entities;
using TransitManager.Mobile.Services;
using TransitManager.Core.DTOs;

namespace TransitManager.Mobile.ViewModels
{
    public partial class ClientsViewModel : ObservableObject
    {
        private readonly ITransitApi _transitApi;
        
        [ObservableProperty]
        private bool _isBusy;

        public ObservableCollection<ClientListItemDto> Clients { get; } = new();

        public ClientsViewModel(ITransitApi transitApi)
        {
            _transitApi = transitApi;
        }

        [RelayCommand]
        private async Task LoadClientsAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                Clients.Clear();
                var clients = await _transitApi.GetClientsAsync();
                foreach (var client in clients)
                {
                    Clients.Add(client);
                }
            }
            catch (System.Exception ex)
            {
                // Gérer l'erreur, par exemple afficher une alerte
                await Shell.Current.DisplayAlert("Erreur", $"Impossible de charger les clients : {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
		
        // MODIFICATION : Le paramètre de la commande est maintenant un DTO
        [RelayCommand]
        private async Task GoToDetailsAsync(ClientListItemDto client)
        {
            if (client == null) return;
            
            await Shell.Current.GoToAsync($"ClientDetailPage?clientId={client.Id}");
        }
    }
}