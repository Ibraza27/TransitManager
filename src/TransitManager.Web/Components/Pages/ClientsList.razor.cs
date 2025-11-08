using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Web.Services;

namespace TransitManager.Web.Components.Pages
{
    public partial class ClientsList
    {
        [Inject]
        private IApiService ApiService { get; set; } = default!;

        protected IEnumerable<Client>? clients;

        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine("[Blazor] ClientsList: OnInitializedAsync - Début du chargement des clients.");
            try
            {
                clients = await ApiService.GetClientsAsync();
                Console.WriteLine($"[Blazor] ClientsList: Chargement terminé. {clients?.Count() ?? 0} client(s) trouvé(s).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Blazor] ClientsList: Erreur lors du chargement des clients : {ex.Message}");
            }
        }
    }
}