using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Threading.Tasks;
using TransitManager.Web.Auth;

namespace TransitManager.Web.Components.Layout
{
    public partial class MainLayout : IDisposable
    {
        [Inject]
        private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        protected override void OnInitialized()
        {
            Console.WriteLine("[Blazor] MainLayout: OnInitialized. Abonnement à l'événement de changement d'état d'authentification.");
            AuthStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }

        private async void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        {
            var state = await task;
            Console.WriteLine($"[Blazor] MainLayout: L'état d'authentification a changé. Utilisateur authentifié : {state.User.Identity?.IsAuthenticated}");
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Logout()
        {
            Console.WriteLine("[Blazor] MainLayout: Demande de déconnexion.");
            var customAuthStateProvider = (CustomAuthenticationStateProvider)AuthStateProvider;
            await customAuthStateProvider.MarkUserAsLoggedOut();
            NavigationManager.NavigateTo("/");
        }

        public void Dispose()
        {
            Console.WriteLine("[Blazor] MainLayout: Dispose. Désabonnement de l'événement.");
            AuthStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }
}