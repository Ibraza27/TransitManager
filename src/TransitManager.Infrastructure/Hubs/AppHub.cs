using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TransitManager.Infrastructure.Hubs
{
    public class AppHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // --- NOUVEAU : Gestion de la frappe ---
        public async Task SendTyping(string groupName, string userName, bool isInternal)
        {
            // On envoie l'info aux autres. 
            // Le client Web filtrera l'affichage si c'est interne et qu'il n'est pas admin.
            await Clients.OthersInGroup(groupName).SendAsync("UserTyping", userName, isInternal);
        }
		
        public async Task SendReadReceipt(string groupName)
        {
            // On informe les autres que tout a été lu dans ce groupe
            await Clients.OthersInGroup(groupName).SendAsync("MessagesRead");
        }
		
    }
}