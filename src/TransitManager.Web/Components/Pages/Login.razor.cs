using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Web.Auth;
using TransitManager.Web.Services;

namespace TransitManager.Web.Components.Pages
{
    public partial class Login
    {
        [Inject]
        private IApiService ApiService { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [SupplyParameterFromForm]
        protected LoginRequestDto loginModel { get; set; } = new();

        // --- DÉBUT DES MODIFICATIONS ---
        [SupplyParameterFromQuery]
        private string? ReturnUrl { get; set; }
        // --- FIN DES MODIFICATIONS ---

        protected string errorMessage = string.Empty;
        protected bool isBusy = false;
        protected bool showPassword = false;
        protected string passwordInputType = "password";

        protected void TogglePasswordVisibility()
        {
            showPassword = !showPassword;
            passwordInputType = showPassword ? "text" : "password";
        }

        protected async Task HandleLogin()
        {
            isBusy = true;
            errorMessage = string.Empty;
            await InvokeAsync(StateHasChanged);

            var result = await ApiService.LoginAsync(loginModel);
            
            if (result != null && result.Success && !string.IsNullOrEmpty(result.Token))
            {
                var customAuthStateProvider = (CustomAuthenticationStateProvider)AuthStateProvider;
                await customAuthStateProvider.MarkUserAsAuthenticated(result.Token);
                
                // --- MODIFIER CETTE LIGNE ---
                NavigationManager.NavigateTo(ReturnUrl ?? "/", forceLoad: true);
            }
            else
            {
                errorMessage = result?.Message ?? "Une erreur est survenue.";
            }

            isBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}