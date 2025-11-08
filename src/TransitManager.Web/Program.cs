using Microsoft.AspNetCore.Components.Authorization;
using TransitManager.Web.Auth;
using TransitManager.Web.Services;
using TransitManager.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES ---

// 1. Enregistre les services Blazor Server (y compris IJSRuntime)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. Configure l'authentification pour gérer les redirections
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });

// 3. Services d'autorisation standards
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// 4. Services personnalisés
// IMPORTANT : Ces services dépendent de IJSRuntime, ils doivent être Scoped
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<AuthHeaderHandler>(); // CHANGEMENT ICI: Scoped au lieu de Transient

// 5. Configuration du HttpClient
var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrEmpty(apiBaseUrl))
{
    throw new InvalidOperationException("ApiBaseUrl is not configured in appsettings.json");
}

builder.Services.AddHttpClient<IApiService, ApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<AuthHeaderHandler>();


var app = builder.Build();

// --- PIPELINE ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Le middleware d'authentification est nécessaire pour que la redirection [Authorize] fonctionne
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();