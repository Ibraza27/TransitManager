// Remplacez TOUT le contenu du fichier par ce code.
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Interfaces;

namespace TransitManager.Infrastructure.Services
{
    public class NotificationHubService : INotificationHubService, IAsyncDisposable
    {
        private readonly HubConnection _hubConnection;
        private bool _isConnected;

        public NotificationHubService()
        {
            // L'URL pointe vers l'API que nous avons créée.
            // Il faudra peut-être ajuster le port si le vôtre est différent.
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7001/notificationHub") 
                .WithAutomaticReconnect()
                .Build();

            _ = Task.Run(ConnectWithRetryAsync);
        }

        private async Task ConnectWithRetryAsync()
        {
            while (true)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    _isConnected = true;
                    Console.WriteLine("SignalR Hub connected.");
                    return;
                }
                catch
                {
                    Console.WriteLine("SignalR Hub connection failed. Retrying in 5 seconds...");
                    await Task.Delay(5000);
                }
            }
        }

        public async Task NotifyClientUpdated(Guid clientId)
        {
            if (!_isConnected) return;
            try
            {
                // Le nom "ClientUpdated" doit correspondre à ce que le client écoute.
                await _hubConnection.InvokeAsync("ClientUpdated", clientId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending SignalR message: {ex.Message}");
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