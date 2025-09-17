using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Messages;

namespace TransitManager.WPF.Helpers
{
    public class SignalRClientService : IAsyncDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly IMessenger _messenger;

        public SignalRClientService(IMessenger messenger)
        {
            _messenger = messenger;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7001/notificationHub") // Doit correspondre à l'URL du service
                .WithAutomaticReconnect()
                .Build();

            // S'abonner aux événements reçus du Hub
            _hubConnection.On<Guid>("ClientUpdated", (clientId) =>
            {
                // Quand on reçoit un message du Hub, on le relaie via le Messenger local.
                _messenger.Send(new ClientUpdatedMessage(true)); 
            });
            // Ajoutez d'autres .On<T>() ici pour les colis, véhicules, etc.
        }

        public async Task StartAsync()
        {
            try
            {
                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                // Gérer les erreurs de connexion initiales
                Console.WriteLine($"Initial SignalR connection error: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}