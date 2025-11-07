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

        protected LoginRequestDto loginModel = new();
        protected string errorMessage = string.Empty;
        protected bool isBusy = false;
        protected bool showPassword = false;
        protected string passwordInputType = "password";

        protected void TogglePasswordVisibility()
        {
            showPassword = !showPassword;
            passwordInputType = "password";
            if (showPassword)
            {
                passwordInputType = "text";
            }
        }

        protected async Task HandleLogin()
        {
            isBusy = true;
            errorMessage = string.Empty;
            StateHasChanged();

            // --- LOG DE DÉBOGAGE CRUCIAL ---
            Console.WriteLine($"[Blazor] HandleLogin déclenché. Envoi des données : Email='{loginModel.Email}', Password='{loginModel.Password}'");

            LoginResponseDto? result = null;
            try
            {
                result = await ApiService.LoginAsync(loginModel);
            }
            catch (Exception ex)
            {
                errorMessage = $"Une erreur de communication est survenue: {ex.Message}";
                isBusy = false;
                StateHasChanged();
                return;
            }
            
            if (result != null && result.Success && !string.IsNullOrEmpty(result.Token))
            {
                var customAuthStateProvider = (CustomAuthenticationStateProvider)AuthStateProvider;
                await customAuthStateProvider.MarkUserAsAuthenticated(result.Token);
                NavigationManager.NavigateTo("/");
            }
            else
            {
                errorMessage = result?.Message ?? "Une erreur est survenue.";
            }

            isBusy = false;
            StateHasChanged();
        }
    }
}